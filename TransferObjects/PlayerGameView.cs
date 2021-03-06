using System;
using System.Linq;
using System.Collections.Generic;

namespace ForgottenArts.Commerce.Server
{
	public class PlayerGameView
	{
		public string Name {get; set;}
		public string Photo {get; set;}
		public string Color {get; set;}
		public int Score {get; set;}
		public long Id { get; set; }
		public GameState Status { get; set; }
		public IEnumerable<HexView> Hexes { get; set; }
		public IEnumerable<string> Hand { get;	set; }
		public Stack<string> Discards {	get; set; }
		public int DeckSize { get;	set; }
		public IEnumerable<string> NationCards { get; set; }
		public List<string> TechnologyCards { get;	set; }
		public TurnView CurrentTurn { get; set; }
		public IEnumerable<StoreCardView> Bank { get; set; }
		public IEnumerable<LogEntry> Log { get; set; }
		public IEnumerable<PlayerSummaryView> Players { get; set; }
		public int Gold { get; set; }
		public IEnumerable<string> TradeCards { get; set; }
		public IEnumerable<OtherPlayerView> OtherPlayers {get; set;}
		public WinView Result {get; set;}

		public PlayerGameView (Game game, PlayerGame player)
		{
			var cards = GameRunner.Instance.Cards;
			this.Id = game.Id;
			this.Name = player.Name;
			this.Photo = player.Player.Photo;
			this.Color = player.Color;
			this.Status = game.Status;
			this.Hand = from c in player.Hand orderby c select c;
			this.Discards = player.Discards;
			this.DeckSize = player.Deck.Count;
			this.NationCards = from c in player.NationCards orderby c select c;
			this.TechnologyCards = player.TechnologyCards;
			this.CurrentTurn = new TurnView(game, game.CurrentTurn);
			this.Bank = from kvp in game.Bank orderby cards[kvp.Key].Category, cards[kvp.Key].Cost select new StoreCardView(
				game, player, kvp);
			this.Hexes = from h in player.Hexes select new HexView (h);
			this.Gold = player.Gold;
			this.TradeCards = from c in player.TradeCards where cards[c.Card] != null orderby cards[c.Card].TradeLevel, c.Card select c.Card;
			this.OtherPlayers = from p in game.Players where p != player select new OtherPlayerView (p);
			this.Players = from p in game.Players select new PlayerSummaryView (p);

			if (game.Status == GameState.Finished) {
				this.Result = new WinView (game);
			}
		}
	}
}

