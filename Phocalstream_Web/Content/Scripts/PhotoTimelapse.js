// Script to handle photo timelapse generation functionality

/*
 * POST the photo Ids to the Timelapse method of the Photo controller.
 */
function generateTimelapse(photoIds, timelapseName) {
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
        value: photoIds
    }));

    form.append($('<input/>', {
        type: 'hidden',
        name: 'timelapseName',
        value: timelapseName
    }));
    form.appendTo('body').submit();
}
