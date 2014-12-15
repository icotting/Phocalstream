// Script to handle the lazy loading and resizing the images on the SearchResults page.

// Dependencies:
// ** totalPhotoCount, a global variable containing the total number of photos returned on the page
// ** basePhotoUrl, a global variable containing a url which a photo Id can be appened to which then directs to the photo info page


/*
 * Lazy load the images.
 */
$("img.photo").lazyload({
    threshold: 400,
    effect: "fadeIn"
});

/* Initial slized value, which equals image width. */
var initialSliderValue = 400;

/* Handles showing and sizing the date label. */
var imgDateSmall = false;
var dateSmallSize = 200;
var imgDateHidden = false;
var dateHiddenSize = 100;

/* Scales the image height based on current width. */
var scalar = 267 / 400; // (height / width) of placeholder


/*
 * Initializes the JQuery slider.
 * Min: 25
 * Max: 800
 * OnSlide, setText is called to update the UI with the resulting image size.
 * OnStop, the images are resized, date labels scaled or hidden, and margins recomputed.
 */
$(function () {
    $("#slider").slider({
        orientation: "horizontal",
        range: "min",
        min: 25,
        max: 800,
        value: initialSliderValue,
        slide: function (event, ui) {
            setText(ui.value);
        },
        stop: function (event, ui) {
            initialSliderValue = ui.value;

            setText(ui.value);
            setImage(ui.value);
            scaleOrHideLabel(ui.value);
            computeHolderMargin(ui.value);
        }
    });
});


/*
 * Compute the image height based on its width.
 */
function getImgHeight(width) {
    return scalar * width;
}

/*
 * Called OnSlide to update the UI with the size of photo for current slider selection.
 */
function setText(width) {
    var height = getImgHeight(width);
    $("#img-size").html(width + " x " + Math.round(height) + " Pixels");
}

/*
 * Called from OnStop to set image size.
 */
function setImage(width) {
    var height = getImgHeight(width);
    $(".photo").width(width);
    $(".photo").height(height);
}

/*
 * Controls the scale and visibility of the date labels.
 * Date labels are hidden when the width of the image is <= 'dateHiddenSize' pixels.
 * If the width is between 'dateHiddenSize' and 'dateSmallSize' pixels, the text size is smaller.
 */
function scaleOrHideLabel(width) {
    if (width <= dateSmallSize) {
        $(".img-date").css("font-size", "10px");
        imgDateSmall = true;

        if (width <= dateHiddenSize) {
            $(".img-date").hide();
            imgDateHidden = true;
        }
    }

    if (imgDateHidden && width > dateHiddenSize) {
        $(".img-date").show();
        imgDateHidden = false;
    }

    if (imgDateSmall && width > dateSmallSize) {
        $(".img-date").css("font-size", "14px");
        imgDateSmall = false;
    }
}

/*
 * Computes the required margin to center the photos in the visbile window.
 */
function computeHolderMargin(width) {
    var margin = 3;
    var imgWidth = width + (2 * margin);

    var totalWidth = $("#partial").width();

    var numberOfPhotosAcross = Math.floor(totalWidth / imgWidth);

    // corrects for when there are less photos than can fit on screen
    var usedWidth = Math.min(numberOfPhotosAcross, totalPhotoCount) * imgWidth;
    var remainder = totalWidth - usedWidth;

    $(".ul-holder").css("margin-left", (remainder / 2) + "px");
}


/*
 * If the User resizes the window, readjust the margins
 */
$(window).bind("resize", resizeWindow);
function resizeWindow(e) {
    computeHolderMargin($("img.photo").width());
}

/*
 * Sets a double click listener on each photo to open the photo info page in a new window.
 */
$(function () {
    $("img.photo").dblclick(function () {
        var id = this.id;
        var url = basePhotoUrl + id;
        window.open(url, '_blank');
    });
});


/*
 * Called to initialize the image, text, and margin size
 * based on initial size of window and photo count.
 */
$(function () {
    setText(initialSliderValue);
    setImage(initialSliderValue);
    computeHolderMargin(initialSliderValue);
});
