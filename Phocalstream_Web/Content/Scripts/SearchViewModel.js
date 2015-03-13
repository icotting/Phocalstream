function Month(month, name) {
    var self = this;

    self.month = month;
    self.name = name;
    self.Selected = ko.observable(false);
}

function Hour(hour, name) {
    var self = this;

    self.hour = hour;
    self.name = name;
    self.Selected = ko.observable(false);
}

function asyncComputed(evaluator, owner) {
    var result = ko.observable();

    ko.computed(function () {
        // Get the $.Deferred value, and then set up a callback so that when it's done,
        // the output is transferred onto our "result" observable
        evaluator.call(owner).done(result);
    });

    return result;
}

function ViewModel() {
    var self = this;
    
    // a boolean that marks whether a search parameter has been entered
    self.search = ko.observable(false);

    // controls the toggle for grouping photos by site
    self.group = ko.observable("site");
    self.group.subscribe(function (newSize) {
        self.getPhotos();
    });

    // array containing ids of photos the user selected
    self.selectedPhotos = ko.observableArray();
    self.selectedCount = ko.computed(function() {
        return self.selectedPhotos().length;
    });

    // function to handle selecting a photo
    self.selectPhoto = function (item) {
        var id = item;

        var img = $("#photo-" + id);

        var selected = img.hasClass('bordered');
        img.toggleClass('bordered', !selected);


        if (self.selectedPhotos.indexOf(id) == -1) {
            self.selectedPhotos.push(id);
        }
        else {
            self.selectedPhotos.remove(id);
        }

        return true;
    }

    // handle changes to thumbnail size
    self.sizes = [{name: "Small"}, {name: "Medium"}, {name: "Large"}]; 
    self.size = ko.observable(self.sizes[0]);
    self.size.subscribe(function (newSize) {
        switch (newSize.name) {
            case 'Small' :
                resizeThumbnail(100);
                break;
            case 'Medium':
                resizeThumbnail(200);
                break;
            case 'Large':
                resizeThumbnail(400);
                break;
        }
    });

    // selected collection id
    self.collectionId = ko.observable();
    self.collectionId.subscribe(function (newSize) {
        self.query();
        self.getPhotos();
    });

    self.siteNames = ko.observable();
    self.tagNames = ko.observable();
    self.dates = ko.observable(yearQuery);

    self.selectedMonths = ko.observableArray();
    self.months = ko.observableArray([
        new Month(1, "January"),     new Month(2, "February"),  new Month(3, "March"),
        new Month(4, "April"),       new Month(5, "May"),       new Month(6, "June"),
        new Month(7, "July"),        new Month(8, "August"),    new Month(9, "September"),
        new Month(10, "October"),    new Month(11, "November"), new Month(12, "December")
    ]);

    self.selectedHours = ko.observableArray();
    self.hours = ko.observableArray([
        new Hour(0, "0000"),        new Hour(1, "0100"),        new Hour(2, "0200"),        new Hour(3, "0300"),
        new Hour(4, "0400"),        new Hour(5, "0500"),        new Hour(6, "0600"),        new Hour(7, "0700"),
        new Hour(8, "0800"),        new Hour(9, "0900"),        new Hour(10, "1000"),       new Hour(11, "1100"),
        new Hour(12, "1200"),       new Hour(13, "1300"),       new Hour(14, "1400"),       new Hour(15, "1500"),
        new Hour(16, "1600"),       new Hour(17, "1700"),       new Hour(18, "1800"),       new Hour(19, "1900"),
        new Hour(20, "2000"),       new Hour(21, "2100"),       new Hour(22, "2200"),       new Hour(23, "2300")
    ]);

    self.toggleAssociation = function (item) {
        var selected = item.Selected();
        item.Selected(!selected);

        return true;
    }

    // utilty function to clear the selected months
    clearMonths = function() {
        ko.utils.arrayForEach(self.selectedMonths(), function(month) {
            self.toggleAssociation(self.months()[month - 1]);
        });
        self.selectedMonths.removeAll();
    }

    // utilty function to clear the selected times
    clearTimes = function() {
        ko.utils.arrayForEach(self.selectedHours(), function(hour) {
            self.toggleAssociation(self.hours()[hour.substring(0,2)]);
        });
        self.selectedHours.removeAll();
    }

    // utilty function to clear the selected photos
    clearSelected = function() {
        ko.utils.arrayForEach(self.selectedPhotos(), function (id) {
            var img = $("#photo-" + id);
            var selected = img.hasClass('bordered');
            img.toggleClass('bordered', !selected);
        })
        self.selectedPhotos.removeAll();
    }

    // utility function to create month query string
    self.monthText = ko.computed(function () {
        var monthsSelected = self.selectedMonths();
        monthsSelected.sort(function(a, b){return a-b} );

        var months = ["January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        ]

        var monthNameArray = [];
        for (var i = 0; i < monthsSelected.length; i++) {
            var index = monthsSelected[i];
            monthNameArray.push(months[index - 1]);
        }

        return monthNameArray.join(', ');
    });

    // utility function to create hour query string
    self.hourText = ko.computed(function () {
        var hoursSelected = self.selectedHours();
        hoursSelected.sort();

        return hoursSelected.join(', ');
    });

    // utility function to create hour query string
    self.hourQuery = ko.computed(function () {
        var hours = self.selectedHours();

        var trimmedHours = []
        for (var i = 0; i < hours.length; i++) {
            trimmedHours.push(hours[i].substring(0, 2));
        }

        return trimmedHours.toString();
    });

    // array of photo ids
    self.photos = ko.observableArray();

    self.query = ko.computed(function () {
        var found = false;
        var q = "Searching for photos ";

        if (self.collectionId() != null && self.collectionId() != "") {
            q += "from collection " + self.collectionId() + "";
            found = true;
        }

        if (self.siteNames() != null && self.siteNames().length != 0) {
            if (found) {
                q += ", and "
            }

            var siteSplit = self.siteNames().split(',');

            if (siteSplit.length == 1) {
                q += "from " + siteSplit[0] + "";
            }
            else if (siteSplit.length == 2) {
                q += "from " + siteSplit[0] + " or " + siteSplit[1] + "";
            }
            else {
                q += "from ";
                for (var i = 0; i < siteSplit.length - 1; i++)
                {
                    q += siteSplit[i] + ", ";
                }
                q += " or " + siteSplit[siteSplit.length - 1] + "";
            }
            found = true;
        }

        if (self.tagNames() != null && self.tagNames().length != 0) {
            if (found) {
                q += ", and "
            }
                
            var tagSplit = self.tagNames().split(',');

            if (tagSplit.length == 1) {
                q += "tagged with " + tagSplit[0] + "";
            }
            else if (tagSplit.length == 2) {
                q += "tagged with " + tagSplit[0] + " or " + tagSplit[1] + "";
            }
            else {
                q += "tagged with ";
                for (var i = 0; i < tagSplit.length - 1; i++)
                {
                    q += tagSplit[i] + ", ";
                }
                q += " or " + tagSplit[tagSplit.length - 1] + "";
            }
            found = true;
        }

        if (self.dates() != null && self.dates().length != 0) {
            if (found) {
                q += ", and "
            }

            var dateSplit = self.dates().split(',');

            if (dateSplit.length == 1) {
                q += "taken on " + dateSplit[0] + "";
            }
            else if (dateSplit.length == 2) {
                q += "taken on " + dateSplit[0] + " or " + dateSplit[1] + "";
            }
            else {
                q += "taken on ";
                for (var i = 0; i < dateSplit.length - 1; i++)
                {
                    q += dateSplit[i] + ", ";
                }
                q += " or " + dateSplit[dateSplit.length - 1] + "";
            }
            found = true;
        }

        if (self.selectedMonths().length != 0) {
            if (found) {
                q += ", and "
            }

            var monthSplit = self.monthText().split(',');

            if (monthSplit.length == 1) {
                q += "taken during the month of " + monthSplit[0] + "";
            }
            else if (monthSplit.length == 2) {
                q += "taken during the months of " + monthSplit[0] + " or " + monthSplit[1] + "";
            }
            else {
                q += "taken during the months of ";
                for (var i = 0; i < monthSplit.length - 1; i++)
                {
                    q += monthSplit[i] + ", ";
                }
                q += " or " + monthSplit[monthSplit.length - 1] + "";
            }
            found = true;
        }

        if (self.selectedHours().length != 0) {
            if (found) {
                q += ", and "
            }

            var hourSplit = self.hourText().split(',');

            if (hourSplit.length == 1) {
                q += "taken during the hour of " + hourSplit[0] + "";
            }
            else if (hourSplit.length == 2) {
                q += "taken during the hours of " + hourSplit[0] + " or " + hourSplit[1] + "";
            }
            else {
                q += "taken during the hours of ";
                for (var i = 0; i < hourSplit.length - 1; i++)
                {
                    q += hourSplit[i] + ", ";
                }
                q += " or " + hourSplit[hourSplit.length - 1] + "";
            }
            found = true;
        }

        if (found) {
            return q;
        }
        else {
            return "Searching for all photos";
        }
    });

    self.reset = function () {
        // reset current list of ids
        photoIds = [];
        self.photos.removeAll();
        self.selectedPhotos.removeAll();

        visibleItems = "";
        totalPhotoCount = 0;

        // remove current photos
        $("#ul-holder").empty();
        $("#ul-holder").data("minOffset", 0);
        $("#ul-holder").data("maxOffset", 0);
    };

    self.queryResults = asyncComputed(function () {
        self.reset();

        return $.ajax("/api/search/count", {
            data: {
                collectionId: this.collectionId,
                hours: this.hourQuery(),
                months: this.selectedMonths().toString(),
                sites: this.siteNames,
                tags: this.tagNames,
                dates: this.dates,
                group: this.group()
            }
        });
    }, this);
    self.queryResults.extend({ notify: 'always' });
        
    self.getPhotos = function() {
        if (self.query() == "Searching for all photos") {
            self.search(false);
            self.reset();
        }
        else {
            self.search(true);

            $.ajax("/api/search/getphotos", {
                data: {
                    collectionId: this.collectionId,
                    hours: this.hourQuery(),
                    months: this.selectedMonths().toString(),
                    sites: this.siteNames,
                    tags: this.tagNames,
                    dates: this.dates,
                    group: this.group()
                },
                error: function(jqXHR, textStatus, errorThrown) {
                    alert(textStatus + ': ' + errorThrown);
                },
                success: function(data) {
                    // Split the data into an array of photo ids
                    var ids = data.split(',');
                    
                    photoIds = ids;
                    visibleItems = data;
                    totalPhotoCount = ids.length;

                    // Add the new photos to the array, then let Knockout know the value changed
                    var array = self.photos();
                    ko.utils.arrayPushAll(array, ids);
                    self.photos.valueHasMutated();

                    // Kick off the lazy loading
                    checkListItemContents($("#ul-holder"));

                    // Initialize the views for proper size
                    initialize();
                }
            });
        }
    }

    self.selectedMonths.subscribe(function(newValue) {
        self.getPhotos();
    });
    self.selectedHours.subscribe(function(newValue) {
        self.getPhotos();
    });
    self.siteNames.subscribe(function(newValue) {
        self.getPhotos();
    });
    self.tagNames.subscribe(function(newValue) {
        self.getPhotos();
    });
    self.dates.subscribe(function(newValue) {
        self.getPhotos();
    });

    self.timelapse = function() {
        var timelapseIds = "";

        if (self.selectedCount() == 0) {
            timelapseIds = visibleItems;
        }
        else {
            timelapseIds = self.selectedPhotos().join(",");
        }

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
            value: timelapseIds
        }));
        form.appendTo('body').submit();
    }

    self.saveCollection = function() {
        if (self.selectedCount() == 0) {
            saveIds = visibleItems;
        }
        else {
            saveIds = self.selectedPhotos().join(",");
        }
        saveCollectionPrompt();
    }

    self.download = function() {
        bootbox.confirm("Are you sure you want to download the images? The download may take some time. A download link will be sent when the process has finished.", function (result) {
            if (result) {
                var downloadIds = "";

                if (self.selectedCount() == 0) {
                    downloadIds = visibleItems;
                }
                else {
                    downloadIds = self.selectedPhotos().join(",");
                }

                $.ajax({
                    url: '/api/sitecollection/RawDownload',
                    data: "photoIds=" + downloadIds,
                    dataType: "string",
                    type: "POST",
                    error: function (jqXHR, textStatus, errorThrown) {
                        alert(errorThrown);
                    }
                })
            }
        });
    }
}

