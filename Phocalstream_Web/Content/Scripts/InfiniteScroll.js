// I get more list items and either prepend them or append
// them to the list depending on the target area.
function getMoreListItems(container, targetArea, onComplete){

    var chunkSize = 1;

    var minIndex = container.data("minOffset");
    var maxIndex = container.data("maxOffset");

    if (minIndex == null | maxIndex == null) {
        console.log("Null index");
        minIndex = -1;
        maxIndex = -1;
    }

    if (targetArea == "top"){
        var nextOffset = (minIndex - 1);
    } else {
        var nextOffset = (maxIndex + 1);
    }

    if (photoIds.length > 0) {
        var slice = photoIds.slice(nextOffset, nextOffset + chunkSize);
        console.log(slice);
        applyListItems(container, targetArea, slice, nextOffset);
        onComplete();
    }
}

function applyListItems( container, targetArea, items, start){
    var template = $("#list-item-template");

    // Create an array to hold our HTML buffer - this will
    // be faster than creating individual DOM elements and
    // appending them piece-wise.
    // var image = "";

    // Loop over the array to create each list element.
    $.each(items, function (index, item) {
        // Modify the template and append the result
        // to the HTML buffer.
        var image = $(template.html().replace(new RegExp("\\$\\{(id)\\}", "g"), item));
        image.data("index", start + index);

        //        }
        //  );

        // Create a list chunk which will hold our data.
        //    var chunk = $( "<div class='list-chunk'></div>" );
        //var chunk = $("");

        // Append the list item html buffer to the chunk.
        //  chunk.append( htmlBuffer.join( "" ) );

        // Create the min and max offset of the chunk.
        // chunk.data( "minOffset", min );
        // chunk.data( "maxOffset", max );

        // Check to see which target area we are adding the
        // list items to (top vs. bottom).
        if (targetArea == "top") {

            // Get the current window scroll before we update
            // the list contente.
            var topBefore = container.scrollTop();

            // Prepend list items.
            container.prepend(image);
            container.scrollTop(topBefore + image.height());

            // Now that we moved the list up, let's remove
            // the last chunk from the list.
            //        container.find( "> div.list-chunk:last" ).remove();

            if (container.children().size() > 100) {
                var oldChunk = container.children(":last");

                oldChunkBottom = oldChunk.offset().top + oldChunk.height();
                oldChunk.remove();
                
//                var viewBottom = $(window).
            }

        } else {

            var topBefore = container.scrollTop();

            console.log("BEFORE: ScrollHeight: " + container.prop('scrollHeight') + ", ScrollTop: " + container.scrollTop());

            // Append list items.
            container.append(image);
            container.scrollTop(topBefore - image.height());

            console.log("AFTER: ScrollHieght: " + container.prop('scrollHeight') + ", ScrollTop: " + container.scrollTop());
            
            if (container.children().size() > 100) {
                var oldChunk = container.children(":first");

                console.log("BEFORE: ScrollHeight: " + container.prop('scrollHeight') + ", ScrollTop: " + container.scrollTop());

                var oldChuckTop = oldChunk.offset().top;
                oldChunk.remove();

                console.log("AFTER: ScrollHieght: " + container.prop('scrollHeight') + ", ScrollTop: " + container.scrollTop());

                //var viewTop = container.scrollTop();
                //var difference = (-oldChuckTop + 3 + container.offset().top) - viewTop;

//                container.scrollTop(viewTop - difference);
            }
        }
    });

    container.data("minOffset", container.children( ":first" ).data( "index" ));
    container.data("maxOffset", container.children( ":last" ).data( "index" ));
}


// I check to see if more list items are needed based on
// the scroll offset of the window and the position of
// the container. I return a complex result that not only
// determines IF more list items are needed, but on what
// end of the list.
//
// NOTE: These calculate are based ONLY on the offset of
// the list container in the context of the view frame.
// This does not take anything else into account (more
// business logic might be required to see if loading
// truly needs to take place).
function isMoreListItemsNeeded( container ){

    var result = {
        top: false,
        bottom: false
    };

    // Get the view frame for the window - this is the
    // top and bottom coordinates of the visible slice of
    // the document.
    var viewTop = $( window ).scrollTop();
    var viewBottom = (viewTop + $( window ).height());

    // Get the offset of the top of the list container.
    var containerTop = container.scrollTop();
    var containerBottom = container.prop('scrollHeight') - containerTop;
    // var containerBottom = Math.floor(containerTop + container.height());

    // NOTE: The top buffer is a bit bigger only to make
    // the transition feel a bit *safer*.
    var topScrollBuffer = 1000;
    var bottomScrollBuffer = 500;

//    console.log("viewBottom: " + viewBottom + ", containerTop: " + containerTop + ", containerBottom: " + containerBottom);

    // TOP
    if (topMoved && containerTop < bottomScrollBuffer){
        result.top = true;
    }
    // Check for the view to have moved out of the 
    // threshhold before trying to load above it
    else if (containerTop > topScrollBuffer) {
        topMoved = true;
    }

    // BOTTOM
    if ((containerBottom - bottomScrollBuffer) <= viewBottom){
        result.bottom = true;
    }

//    console.log(result);

    // Return the requirments for the loading.
    return( result );
}


// I check to see if more list items are needed, and, if
// they are, I load them.
function checkListItemContents( container ){
    // Check to see if more items need to be loaded at
    // the top or the bottom (based purely on position).
    // Returns: { top: boolean, bottom: boolean }.
    var isMoreLoading = isMoreListItemsNeeded( container );

    // Define an onComplete method for the AJAX load that
    // will call this method again to make sure there is
    // always enough data loaded on the page.
    var onComplete = function () {
        checkListItemContents(container);
    };

    // Check to see if more list items are needed at the
    // top. If so, we will check to offsets to see if the
    // load needs to take place.
    //
    // NOTE: Position is only *part* of how we determine
    // if additional content is needed at the top.

    var min = (container.data("minOffset") || 0);
    var max = (container.data("maxOffset") || 0);

    if (isMoreLoading.top && container.data("minOffset") &&
            (container.data("minOffset") > 0)) {

        // Load and prepend more list items.
        getMoreListItems(container, "top", onComplete);

        // Check to see if more list items are needed at the
        // bottom. For this, all we are going to rely on is
        // the offset of the container (since we can load
        // ad-infinitum in the bottom direction).
    } else if (isMoreLoading.bottom && (max < totalPhotoCount - 1)){
        // Load and append more list items.
        getMoreListItems(container, "bottom", onComplete);
    }
}


// -------------------------------------------------- //
// -------------------------------------------------- //


// When the DOM is ready, initialize document.
jQuery(function( $ ){

    // Get a reference to the list container.
    var container = $("#ul-holder");

    // Bind the scroll and resize events to the window.
    // Whenever the user scrolls or resizes the window,
    // we will need to check to see if more list items
    // need to be loaded.
    $( window ).bind("scroll resize", function( event ){
            // Hand the control-flow off to the method
            // that worries about the list content.
            checkListItemContents( container );
        }
    );

    $("#ul-holder").bind("scroll", function (event) {
            checkListItemContents(container);
        }
    );

    // Now that the page is loaded, trigger the "Get"
    // method to populate the list with data.
    //checkListItemContents( container );

});
