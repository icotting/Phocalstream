$(document).ready(function () {

    var mapOptions = {
        credentials: "AvikYUpjcBjzTZDMq41HatI4Val4on1Qp45zwxkOVVEB_tQ-dP-fpTuV5axCkTkW",
        mapTypeId: Microsoft.Maps.MapTypeId.road,
        showDashboard: true,
        width: 600,
        height: 400
    }

    var map = new Microsoft.Maps.Map(document.getElementById("map"), mapOptions);
    var locations = new Array();

    $.each(siteList, function (i, site) {
        var loc = new Microsoft.Maps.Location(site.Site.Latitude, site.Site.Longitude);
        locations.push(loc);
        var pin = new Microsoft.Maps.Pushpin(loc);
        pin.siteId = site.Site.ID;
        Microsoft.Maps.Events.addHandler(pin, 'click', function (e) {
            $("#siteList").load('/Home/SiteDetails/' + e.target.siteId);
        });

        map.entities.push(pin);
    });

    map.setView({
        animate: true,
        bounds: Microsoft.Maps.LocationRect.fromLocations(locations)
    });
});