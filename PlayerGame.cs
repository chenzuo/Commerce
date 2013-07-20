using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ForgottenArts.Commerce
{
	public class PlayerGame
	{
		public int HandSize = 5;

		public string PlayerKey {get; set;}
		public long GameId {get; set;}
		public int Score {get; set;}
		public int Gold {get; set;}

		public Stack<string> Deck {get; set;}
		public List<string> Hand {get; set;}
		public Stack<string> Discards {get; set;}

		public List<string> TradeCards {get; set;}
		public List<string> TechnologyCards {get; set;}

		public List<Match> ProposedMatches {get; set; }
		public List<Match> ReceivedMatches {get; set; }
		public bool DoneTrading { get; set; }

		public List<Hex> Hexes {get; set;}

		public List<LogEntry> Log { get; set; }

		public PlayerGame () {
			Deck = new Stack<string> ();
			TradeCards = new List<string> ();
			TechnologyCards = new List<string> ();
			Hand = new List<string> ();
			Discards = new Stack<string> ();
			Log = new List<LogEntry> ();
			Hexes = new List<Hex> ();
			ProposedMatches = new List<Match> ();
			ReceivedMatches = new List<Match> ();
		}

		public IEnumerable<string> AllCards {
			get {
				foreach (var card in Deck) {
					yield return card;
				}
				foreach (var card in Hand) {
					yield return card;
				}
				foreach (var card in Discards) {
					yield return card;
				}
				foreach (var card in TradeCards) {
					yield return card;
				}
			}
		}

		public void AddHex (int populationLimit, int currentPopulation = 0)
		{
			var hex = new Hex () {
				Id = GameRunner.Instance.Repository.NewId (),
				PopulationLimit = populationLimit,
				CurrentPopulation = currentPopulation,
				HasColony = false
			};
			this.Hexes.Add (hex);
		}

		public bool Defends () 
		{
			//TODO: scan cards in hand, looking for card with defensive actoion
			// and call it!
			return false;
		}

		public void DrawHand () {
			while (Hand.Count < HandSize) {
				if (Deck.Count == 0) {
					if (Discards.Count == 0) {
						//no more cards to draw.
						return;
					}
					// Shuffle discard.
					var discards = new List<string> (Discards);
					discards.Shuffle ();
					Deck = new Stack<string> (discards);
					Discards.Clear ();
				}
				Hand.Add (Deck.Pop());
			}
		}

		public bool HasTradeCards (List<string> cards)
		{
			// There's probably a more optimal way to do this.
			var tradeCards = new List<string>(this.TradeCards);
			foreach (string card in cards) {
				if (!tradeCards.Contains(card)) {
					return false;
				}
				tradeCards.Remove(card);
			}
			return true;
		}

		public void ReceiveMatch (Match match)
		{
			this.ReceivedMatches.Add (match);
		}

		private Player player;
		[IgnoreDataMember]
		public Player Player {
			get {
				if (player == null) {
					player = Player.GetOrCreate (this.PlayerKey);
				}
				return player;
			}
			set {
				player = value;
			}
		}

		private Game game;
		[IgnoreDataMember]
		public Game Game {
			get {
				if (game == null) {
					game = GameRunner.Instance.Repository.Get<Game>(Game.GetKey(GameId));
				}
				return game;
			}
			set {
				game = value;
			}
		}

		public void PublishLogEntry (LogEntry entry)
		{
			this.Log.Add (entry);
		}

		public void Send (object message, string channel)
		{
			PlayerSocketServer.Instance.Send (message, channel, this);
		}

		public override string ToString ()
		{
			if (Player != null) {
				return Player.ToString();
			}
			else {
				return "Unkown Player";
			}
		}

		public string GetKey ()
		{
			return (GameId + "-" + PlayerKey);
		}

		public static string MakeKey (long gameId, string playerKey)
		{
			return gameId + "-" + playerKey;
		}
	}
}

