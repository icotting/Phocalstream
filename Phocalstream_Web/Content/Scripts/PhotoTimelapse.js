// Script to handle photo timelapse generation functionality

// Dependencies:
// ** visibleItems, a global variable containing a comma-separated list of photo Ids

/*
 * POST the photo Ids to the Timelapse method of the Photo controller.
 */
function generateTimelapse() {
    var form;
    form = $('<form />', {
        action: '/photo/timelapse',
        method: 'POST',
        target: '_blank',
        style: 'display: none;'
    });

    form.append($('<input/>', {
        type: 'hidden',
        name: 'photoIds',
        value: visibleItems
    }));
    form.appendTo('body').submit();
}
