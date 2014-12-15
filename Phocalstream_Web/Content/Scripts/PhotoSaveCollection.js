// Script to handle saving photos to a user defined collection

// Dependencies:
// ** visibleItems, a global variable containing a comma-separated list of photo Ids
// ** #userCollectionModal, a bootstrap modal that presents the existing user collections
//          and allows users to create a new collection      


/*
 * Handles user selection on the list of existing user defined collections.
 */
var collectionIds = [];
function registerCollectionSelection(collectionId) {
    if (jQuery.inArray(collectionId, collectionIds) == -1) {
        collectionIds.push(collectionId);
    }
    else {
        var index = collectionIds.indexOf(collectionId);
        collectionIds.splice(index, 1);
    }
}

/*
 * Hides the SilverLight viewer, if applicable, and presents the user with a modal
 * to create a new collection add/or add photos to existing collections.
 */
function saveCollectionPrompt() {
    $("#silverlightControlHost").hide();

    $('#userCollectionModal').modal({
        show: true,
        backdrop: 'static',
        closeOnEscape: true
    });
}

/*
 * Handles Cancel modal button click
 * The modal is dismissed and, if applicable, the SilverLight viewer is shown.
 */
function cancelCollectionModalButton() {
    $("#silverlightControlHost").show();
}

/*
 * Handles Save modal button click
 * If a New Collection name is provided, saveCollection() is called to create this new collection.
 * If existing collections are selected, addToCollection() is called to add photos to those collections.
 * The modal is dismissed and, if applicable, the SilverLight view is shown.
 */
function saveCollectionModalButton() {
    var newName = $('#newCollectionName').val();
    if (newName != "") {
        saveCollection(newName);
    }

    if (collectionIds.length > 0) {
        addToCollection(collectionIds);
    }

    $("#silverlightControlHost").show();
}

/*
 * POST the collection Ids and photo Ids to the User Collection API contoller.
 */
function addToCollection(collectionIds) {
    $.ajax({
        url: '/api/usercollection/AddToCollection?collectionIds=' + collectionIds.join(',') + '&photoIds=' + visibleItems,
        type: 'POST'
    });
}

/*
 * POST the photo Ids and New Collection Name to the User Collection API controller.
 */
function saveCollection(collectionName) {
    $.ajax({
        url: '/api/usercollection/SaveUserCollection?photoIds=' + visibleItems + '&collectionName=' + collectionName,
        type: "POST"
    });
}

/*
 * Register a click listener on the modal <li> elements to toggle selection.
 */
$(document).ready(function () {
    $('.list-group-item').click(function () {
        $(this).toggleClass('list-group-item-success');

        registerCollectionSelection($(this).attr('id'));
    });
});