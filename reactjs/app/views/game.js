/** @jsx React.DOM */
define(['react', 'jsx/card', 'jsx/game-list', 'main', 'jquery', 'game', 'jsx/game-view'], 
    function (React, Card, GameList, Plus, $, gameServer, GameView) {
  var currentGame = null;
  var game = {
    render: function(game) {
      currentGame = game;
      Plus.ready(function() {
        console.log ('currentGame: ', currentGame);
        var renderOutput = null;
        if (currentGame) {
          renderOutput = (<GameView game={currentGame} />);
        } else {
          renderOutput = (<div>
              <GameList />
              <Card name="One Colonist"/>
            </div>);
        }
        React.renderComponent(renderOutput, document.getElementById("output"));
      });
    }
  };

  return game;
});