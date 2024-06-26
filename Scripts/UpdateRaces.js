$(document).ready(function () {
    function updateRaces() {
        $.ajax({
            url: '/Race/GetRaceUpdates',
            type: 'GET',
            success: function (data) {
                var raceTable = $('#raceTableBody');
                raceTable.empty();

                data.forEach(function (race) {
                    var raceRow = '<tr>' +
                        '<td>' + race.RaceId + '</td>' +
                        '<td>' + new Date(race.StartTime).toLocaleString() + '</td>' +
                        '<td>';

                    race.Horses.forEach(function (horse) {
                        raceRow += '<p>' + horse.Name + ' (WinnerOdds: ' + horse.WinnerOdds + ')' + (horse.IsWinner ? ' - <b>Winner!</b>' : '') + '</p>';
                    });

                    raceRow += '</td>' +
                        '<td>' +
                        '<a href="/Race/PlaceBet?raceId=' + race.RaceId + '">Place Bet</a> | ' +
                        '<a href="/Race/SimulateRace?raceId=' + race.RaceId + '">Simulate Race</a>' +
                        '</td>' +
                        '</tr>';

                    raceTable.append(raceRow);
                });
            }
        });
    }

    setInterval(updateRaces, 5000);
    updateRaces(); // Initial call to populate the table immediately
});
