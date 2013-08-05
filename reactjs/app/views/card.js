/** @jsx React.DOM */
define(['react', 'game', 'pubsub'], function (React, gameServer, Events) {

  var cards = null;
  var waitingCards = [];

  Events.subscribe('game', function(game) {
    gameServer.getCards(function(resp) {
      cards = {};
      for (var i = 0; i < resp.length; i++) {
        var card = resp[i];
        cards[card.name] = card;
      }
      for (var i = 0; i < waitingCards.length; i++) {
        var card = waitingCards[i];
        try {
          card.setState(cards[card.props.name]);
        }
        catch (e) {
          console.error(e);
        }
      }
      waitingCards = [];
    });
  });

  var Card = React.createClass ({
    getInitialState: function () {
      if (!cards) {
        waitingCards.push(this);
        return {};
      }
      return cards[this.props.name] || {};
    },

    render: function () {
      var elems = [];
      elems.push(<header>{this.state.name}</header>);
      if (this.state.imageUrl)
        elems.push(<img src={this.state.imageUrl}/>)
      if (this.state.description) 
        elems.push(<p>{this.state.description}</p>)
      return <div class="card">{elems}</div>;
    }
  });

  return Card;
});
