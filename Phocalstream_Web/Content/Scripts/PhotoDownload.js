// Script to handle photo download functionality

// Dependencies:
// ** visibleItems, a global variable containing a comma-separated list of photo Ids
// ** bootbox.js, a libarary that provides simple Bootstrap modals


/* 
 * Hides the SilverLight Viewer, if applicable, and presents the user with a confirmation dialog.
 */
function downloadPrompt() {
    $("#silverlightControlHost").hide();
    bootbox.confirm("Are you sure you want to download the images? The download may take some time. A download link will be sent when the process has finished.", function (result) {
        if (result) {
            downloadImages();
            $("#silverlightControlHost").show();
        }
        else {
            $("#silverlightControlHost").show();
        }
    });
}

/*
 * POST the photo ids to the download api controller method.
 */
function downloadImages() {
    $.ajax({
        url: '/api/sitecollection/RawDownload?photoIds=' + visibleItems,
        type: "POST"
    })
}
