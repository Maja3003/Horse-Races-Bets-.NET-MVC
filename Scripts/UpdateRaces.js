$(document).ready(function () {
    var connection = new signalR.HubConnectionBuilder().withUrl("/bettingHub").build();

    connection.on("ReceiveBet", function (bet) {
        console.log("New bet placed:", bet);
    });

    connection.on("ReceiveRaceUpdate", function (raceId, winningHorseId) {
        console.log("Race updated:", raceId, winningHorseId);
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });

    $('.bet-option').on('click', function (e) {
        e.preventDefault();
        var horseName = $(this).data('horse');
        var odds = $(this).data('odds');
        var betType = $(this).data('bettype');

        var betItem = `
            <div class="bet-item">
                <div class="bet-details">
                    <span>${horseName}</span>
                    <span>${betType}</span>
                </div>
                <div class="bet-odds">${odds}</div>
                <img src="https://png.pngtree.com/png-vector/20190326/ourmid/pngtree-vector-trash-icon-png-image_865253.jpg" class="trash-icon delete-bet" alt="Delete">
            </div>`;

        $('#bet-list').append(betItem);
    });

    $(document).on('click', '.delete-bet', function () {
        $(this).parent('.bet-item').remove();
    });

    $('.place-bet-button').on('click', function () {
        var amount = $('.bet-amount-input').val();
        if (!amount) {
            alert('Please enter a bet amount.');
            return;
        }

        var userName = '@User.Identity.Name'; // Replace with actual method to get current user

        if (!userName) {
            window.location.href = '/Login'; // Redirect to login page if not logged in
            return;
        }

        var bets = [];
        $('#bet-list .bet-item').each(function () {
            var horseName = $(this).find('.bet-details span:first').text();
            var betType = $(this).find('.bet-details span:last').text();
            var odds = $(this).find('.bet-odds').text();
            bets.push({ HorseName: horseName, BetType: betType, Odds: odds });
        });

        if (bets.length === 0) {
            alert('Please add at least one bet.');
            return;
        }

        var raceId = $('.tab.active').data('tab'); // Get the raceId from the active tab

        $.ajax({
            url: '/Race/PlaceBet',
            type: 'POST',
            data: JSON.stringify({ raceId: raceId, bets: bets, userName: userName, amount: amount }),
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                alert('Bet placed successfully.');
                $('#bet-list').empty();
                $('.bet-amount-input').val('');
            },
        });
    });
});
