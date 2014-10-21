var FRAME_RATE = 75;
var MAX_BUFFER = 500; // buffer size

var dmWeeks = new Array();
var weekCount = 0;

var buffer; // the actual Image objects with data for a photo

var pos = 0; // current position for display
var bufferPos = 0; // current position of image to buffer

var len = 0; // length of the image set
var bufferLen = 0; // length of the buffer (could be less than MAX)

var running;

var progLen; // length of the progress bar

var DATE_PATTERN = /\/Date\((.*)\)\//;
var dmChart;
var dmPointer = 0;

$(document).ready(function () {
    for (var w in weeks) {
        var time = DATE_PATTERN.exec(weeks[w])[1];
        var week = new Date(Number(time));
        dmWeeks.push(week);
    }
    weekCount = dmWeeks.length;

    // set the total length
    len = frameset.length;

    // set the progress bar length conditionally based on the image set size
    if (len > MAX_BUFFER) {
        progLen = MAX_BUFFER;
    } else {
        progLen = len;
    }

    var series = [
        {
            name: 'None',
            data: []
        },
        {
            name: 'D0',
            data: []
        },
        {
            name: 'D1',
            data: []
        },
        {
            name: 'D2',
            data: []
        },
        {
            name: 'D3',
            data: []
        },
        {
            name: 'D4',
            data: []
        }
    ];

    dmChart = new Highcharts.Chart({
        chart: {
            renderTo: 'open_container',
            type: 'bar'
        },
        title: {
            text: 'Drought Monitor Data'
        },
        subtitle: {
            text: 'Source: droughtmonitor.unl.edu'
        },
        legend: true,
        xAxis: {
            categories: [
                'US',
                'State',
                'County'
            ]
        },
        yAxis: {
            min: 0,
            max: 100,
            title: false
        },
        tooltip: {
            formatter: function () {
                return '' + this.series.name +
                    ': ' + this.y + '%';
            }
        },
        plotOptions: {
            series: {
                stacking: 'normal'
            }
        },
        series: series
    });

    // create the array to hold the buffered image data
    buffer = new Array();
    bufferData();
});

function loadDmData() {
    $.ajax(
        {
            url: "/api/data/timelapseweek",
            type: "POST",
            data: { CountyFips: fips, Latitude: lat, Longitude: lon, DmWeek: dmWeeks[dmPointer++ % weekCount].toJSON() },
            success: function (results) {
                dmChart.series[0].setData([results.DMData.US.NonDrought, results.DMData.STATE.NonDrought, results.DMData.COUNTY.NonDrought], true);
                dmChart.series[1].setData([results.DMData.US.D0, results.DMData.STATE.D0, results.DMData.COUNTY.DO], true);
                dmChart.series[2].setData([results.DMData.US.D1, results.DMData.STATE.D1, results.DMData.COUNTY.D1], true);
                dmChart.series[3].setData([results.DMData.US.D2, results.DMData.STATE.D2, results.DMData.COUNTY.D2], true);
                dmChart.series[4].setData([results.DMData.US.D3, results.DMData.STATE.D3, results.DMData.COUNTY.D3], true);
                dmChart.series[5].setData([results.DMData.US.D4, results.DMData.STATE.D4, results.DMData.COUNTY.D4], true);

                $("#discharge").empty();
                $("#discharge").append(results.AverageDischarge);
            }
        });
}

function bufferData() {
    // for each image up to MAX, load the data into the buffer
    var imageObj = new Image();

    // event handler to invoke the call to buffer the next image after the current one has been bufferd
    imageObj.onload = function () {

        // update the progress bar
        var complete = parseInt(((bufferPos++ / progLen) * 100));
        $("#progress").css("width", complete + "%").attr('aria-valuenow', complete);

        $("#bufferlabel").empty();
        $("#bufferlabel").append(complete + "%");

        // get the display canvas and set the size
        var canvas = document.getElementById("vscreen");

        canvas.style.width = width + "px";
        canvas.style.height = height + "px";

        canvas.width = width;
        canvas.height = height;

        var context = canvas.getContext('2d');

        // draw the buffered image data
        context.drawImage(this, 0, 0, width, height);

        // if there is more to buffer, do that
        if (bufferPos < len && bufferPos < MAX_BUFFER) {
            bufferData();
        } else {
            // if not, set the length of the buffer
            bufferLen = bufferPos;
            // get rid of the buffering display and begin the movie
            $("#loadDiv").hide();
            $("#controls").show();
            running = true;
            loadDmData();
            nextImage(); // invokes the call to update the display canvas with the next image
        }
    };

    // set the source of the image being buffered which will cause the event handler to be invoked when the image has loaded
    imageObj.src = "/api/photo/" + imageRes + "/" + frameset[bufferPos].PhotoID;
    buffer.push(imageObj); // store the image object in the buffer
}

function nextImage() {
    var imageTime = new Date(Number(DATE_PATTERN.exec(frameset[pos % len].FrameTime)[1]));

    $("#frameDate").empty();
    $("#frameDate").append(imageTime);

    // load the image data to display from the buffer
    var image = buffer[pos++ % bufferLen];

    // get the display canvas drawing context
    var canvas = document.getElementById("vscreen");
    var context = canvas.getContext('2d');

    // draw the buffered image data
    context.drawImage(image, 0, 0, width, height);

    // if the buffer is smaller than the total frameset, buffer the next image
    if (bufferLen < len) {
        var imageObj = new Image();

        /* Load the next image based on the buffer position into the buffer.
         * NOTE: this will place the next image to buffer into the position
         * of the image data that was just displayed. In MAX cycles, this
         * data will be pulled from the buffer and displayed. This gives
         * sufficient time for the data to load before it is needed. */
        imageObj.src = "/api/photo/" + imageRes + "/" + frameset[bufferPos % len].PhotoID;
        buffer[bufferPos++ % bufferLen] = imageObj;
    }

    // set the time to invoke the next image display call
    if (running == true) {
        setTimeout(function () { nextImage(); }, FRAME_RATE);
    }

    if (dmWeeks.length > 1 && imageTime.getTime() >= dmWeeks[dmPointer % weekCount].getTime()) {
        loadDmData();
    }
}

function setFrameRate(rate) {
    FRAME_RATE = rate;
}

function startStop() {
    if (running == true) {
        $("#playbtn").removeClass("glyphicon-pause");
        $("#playbtn").addClass("glyphicon-play");
        running = false;
    } else {
        $("#playbtn").removeClass("glyphicon-play");
        $("#playbtn").addClass("glyphicon-pause");
        running = true;
        nextImage();
    }
}