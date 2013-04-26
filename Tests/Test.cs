using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ForgottenArts.Commerce;

namespace Tests
{
	[TestFixture()]
	public class CardTests
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			ScriptManager.Manager.Setup (Config.DllPath);
		}

		[Test()]
		public void LoadCardsTest ()
		{
			CardCatalog catalog = new CardCatalog();
			catalog.LoadCards("cards");
		}

		[Test()]
		public void PlayCardTest ()
		{
			CardCatalog catalog = new CardCatalog();
			catalog.LoadCards("cards");

			Game g = new Game ();
			PlayerGame p1 = new PlayerGame ();
			PlayerGame p2 = new PlayerGame ();
			p1.Game = g;
			g.Players.Add (p1);
			g.Players.Add (p2);

			g.CurrentTurn.Player = p1;

			p2.Hand.AddRange(new string [] {"General", "Marketplace", "Sawmill", "Scout"});
			catalog.PlayCard (p1, "General");

			Assert.That(p2.Hand.Count, Is.EqualTo(3));
			Assert.That(p2.Hand[0], Is.EqualTo ("Marketplace"));
			Assert.That(p2.Hand[1], Is.EqualTo ("Sawmill"));
			Assert.That(p2.Hand[2], Is.EqualTo ("Scout"));
		}

		[Test]
		public void StartGameTest ()
		{
			Game g = new Game ();
			PlayerGame p1 = new PlayerGame ();
			PlayerGame p2 = new PlayerGame ();
			p1.Game = g;
			g.Players.Add (p1);
			g.Players.Add (p2);

			GameRunner gr = new GameRunner ();

			Assert.That (g.Status, Is.EqualTo (GameState.Starting));

			gr.Start (g);

			Assert.That (g.Status, Is.EqualTo (GameState.Running));
		}

		[Test]
		public void BuyTest () {
			Game g = new Game ();
			PlayerGame p1 = new PlayerGame ();
			PlayerGame p2 = new PlayerGame ();
			p1.Game = g;
			g.Players.Add (p1);
			g.Players.Add (p2);
			
			GameRunner gr = new GameRunner ();
			gr.Start (g);

			p1.Hand = new List<string>() {"Wood", "Wood", "Wheat", "Wheat"};

			gr.Buy (g, p1, "Scouts");

			Assert.That(p1.Hand.Contains ("Scouts"));

			Assert.That(p1.Hand.Sum(c=> c == "Wheat" ? 1 : 0), Is.EqualTo (1));

			Assert.That(p1.Hand.Sum(c=> c == "Wood" ? 1 : 0), Is.EqualTo (1));
		}
	}
}

