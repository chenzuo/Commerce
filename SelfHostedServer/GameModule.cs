using System;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using System.Collections.Generic;

namespace ForgottenArts.Commerce.Server
{
	/* Operations we care about:
	 *  - associate a g+ user to a Player oject: PUT G+ DTO return Player DTO (for now, just put players).
	 *  - start hosting a new game - POST Game DTO return Game DTO (w/ Id or something set) - inc. players
	 */

	public class GameModule : Nancy.NancyModule	
	{
		private IRepository repository;

		void SetupDependencies ()
		{
			repository = GameRunner.Instance.Repository = new RedisRepository ();
		}

		public GameModule () : base ("/api") {
			SetupDependencies();
			After += ctx => ctx.Response.WithHeader("Access-Control-Allow-Origin", "*");
			Options["/{path*}"] = CrossOriginSetup;
			Put["/player/auth"] = AuthenticatePlayer;
			Post["/game"] = CreateGame;
			Get["/player/{id}/games"] = GetGames;
			Get["/game/{game}"] = GetGame;
			Get["/game/{id}/cards"] = GetGameCards;

			// Player phase endpoints.
			Post["/game/{game}/playCard"] = PlayCard;
			Post["/game/{game}/buyCard"] = BuyCard;
			Post["/game/{game}/skip"] = Skip;
			Post["/game/{game}/redeem"] = Redeem;

			// Trade phase endpoints
			Post["/game/{game}/offer"] = PostOffer;
			Post["/game/{game}/match/suggest"] = SuggestMatch;
			Post["/game/{game}/match/accept"] = AcceptMatch;
			Post["/game/{game}/match/cancel"] = CancelMatch;
			Post["/game/{game}/trading/done"] = DoneTrading;
		}

		public dynamic CrossOriginSetup (dynamic parameters)
		{
			return new Response ()
				.WithHeader ("Access-Control-Allow-Origin", "*")
				.WithHeader ("Access-Control-Allow-Headers", "Content-Type, Player")
				.WithHeader ("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
		}

		public dynamic GetGameCards (dynamic arg)
		{
			var game = repository.Get<Game>(Game.GetKey(arg.id));
			if (game == null) {
				throw new Exception ("Couldn't find game.");
			}
			var cardKeys = new HashSet<string> ();
			foreach (var card in game.Bank.Keys) {
				cardKeys.Add (card);
			}
			foreach (var cardSet in game.TradeCards) {
				foreach (var card in cardSet) {
					cardKeys.Add (card);
				}
			}
			foreach (var player in game.Players) {
				foreach (var card in player.AllCards) {
					cardKeys.Add (card);
				}
			}
			return from key in cardKeys select GameRunner.Instance.Cards[key];
		}

		public dynamic AuthenticatePlayer (dynamic parameters)
		{
			var p2 = this.Bind<Player> ();
			repository.Put (p2.GetKey(), p2);
			//TODO: return games.
			return p2;
		}

		public dynamic CreateGame (dynamic parameters)
		{
			var g = this.Bind<NewGameInfo>();
			var game = new Game () {
				Id = repository.NewId()
			};
			
			foreach (string p in g.Players) {
				var player = Player.GetOrCreate(p);
				var gameList = player.GetPlayerGames();
				gameList.Add (game.Id);
				var playerGame = new PlayerGame () {
					PlayerKey = player.PlusId,
					Game = game,
					GameId = game.Id
				};
				game.Players.Add (playerGame);
			}
			GameRunner.Instance.Start (game);
			repository.Put (game.GetKey(), game);
			return new {GameId = game.Id};
		}

		dynamic GetGames (dynamic parameters)
		{
			var player = Player.GetOrCreate (parameters.id);
			var gameList = new List<GameListInfo> ();
			foreach (var gameId in player.GetPlayerGames()) {
				var game = repository.Get<Game>(Game.GetKey(gameId));
				if (game != null) {
					gameList.Add (new GameListInfo(game));
				}
			}
			return gameList;
		}

		dynamic GetGame (dynamic arg)
		{
			var playerKey = this.Request.Headers["Player"].First();
			var game = repository.Get<Game>(Game.GetKey(arg.game));
			var player = game.GetPlayer (playerKey);
			return new PlayerGameView (game, player);
		}

		dynamic GenericAction<T>(long gameId, Func<Game, PlayerGame, T, bool> func)
		{
			var playerKey = this.Request.Headers["Player"].First();
			var game = repository.Get<Game>(Game.GetKey(gameId));
			var player = game.GetPlayer(playerKey);
			var request = this.Bind<T>();
			try
			{
				if (func(game, player, request)) {
					this.repository.Put(game.GetKey(), game);
					if (PlayerSocketServer.Instance != null) {
						foreach (var p in game.Players) {
							object message = null;
							string channel = string.Empty;
							switch (game.Status) {
							case GameState.Trading:
								message = new TradeView (game, p);
								channel = "tradeUpdate";
								break;
							default:
								message = new PlayerGameView (game, p);
								channel = "gameUpdate";
								break;
							}
							PlayerSocketServer.Instance.Send(message, channel, p);
						}
					}
				}
				return string.Empty;
			}
			catch (InvalidOperationException e)
			{
				return new {error = e.Message};
			}
		}

		dynamic PlayCard (dynamic arg)
		{
			return GenericAction<CardRequest>((long)arg.game, (game, player, card) => 
				GameRunner.Instance.PlayCard (game, player, card.Card, card.HexId));
		}

		dynamic BuyCard (dynamic arg)
		{
			return GenericAction<CardRequest>((long)arg.game, (game, player, card) =>
				GameRunner.Instance.Buy (game, player, card.Card));
		}

		dynamic Skip (dynamic arg)
		{
			return GenericAction<SkipRequest>((long)arg.game, (game, player, skip) =>
				GameRunner.Instance.Skip (game, player, skip.Phase));
		}

		dynamic Redeem (dynamic arg)
		{
			return GenericAction<RedeemRequest>((long)arg.game, (game, player, redeem) =>
				GameRunner.Instance.RedeemTradeCards (game, player, redeem.Cards));
		}

		dynamic PostOffer (dynamic arg)
		{
			return GenericAction<OfferRequest>((long)arg.game, (game, player, offer) =>
				GameRunner.Instance.ListOffer (game, player, offer.Cards));
		}

		dynamic SuggestMatch (dynamic arg)
		{
			return GenericAction<SuggestMatchRequest>((long)arg.game, (game, player, suggest) => {
				var offer1 = game.Trades.Find(o => o.Id == suggest.MyOfferId);
				var offer2 = game.Trades.Find(o => o.Id == suggest.OtherOfferId);
				PlayerGame otherPlayer = null;
				if (offer2 != null) {
					otherPlayer = game.GetPlayer (offer2.PlayerKey);
				}
				return GameRunner.Instance.SuggestMatch (game, player, offer1, otherPlayer, offer2);
			});
		}

		dynamic AcceptMatch (dynamic arg)
		{
			return GenericAction<OfferMatchRequest>((long)arg.game, (game, player, req) => {
				var match = player.ReceivedMatches.Find (m => m.MatchId == req.MatchId);
				return GameRunner.Instance.AcceptMatch (game, player, match);
			});
		}

		dynamic CancelMatch (dynamic arg)
		{
			return GenericAction<OfferMatchRequest>((long)arg.game, (game, player, req) => {
				var match = player.ReceivedMatches.Find (m => m.MatchId == req.MatchId);
				if (match == null) {
					match = player.ProposedMatches.Find (m => m.MatchId == req.MatchId);
				}
				return GameRunner.Instance.CancelMatch (game, player, match);
			});  
		}

		dynamic DoneTrading (dynamic arg)
		{
			return GenericAction<EmptyRequest>((long)arg.game, (game, player, empty) =>
				GameRunner.Instance.DoneTrading (game, player));
		}
	}
}
