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

    self.cameraSiteName = ko.observable().extend({ required: true });

    self.cameraAddress = ko.observable();
    self.cameraAddress.subscribe(function (newValue) {
        if (newValue != "") {
            plot('address');
        }
    })

    self.latitude = ko.observable().extend({
        required: true,
        number: true
    });
    self.longitude = ko.observable().extend({ 
        required: true,
        number: true
    });

    self.county = ko.observable().extend({ });
    self.state = ko.observable().extend({});

    self.errorMessage = ko.observable("");

    self.submit = function () {
        if (self.errors().length === 0) {
            var form;
            form = $('<form />', {
                action: '/account/CreateUserSite',
                method: 'POST',
                style: 'display: none;'
            });

            form.append($('<input/>', {
                type: 'hidden',
                name: 'CameraSiteName',
                value: self.cameraSiteName()
            }));

            form.append($('<input/>', {
                type: 'hidden',
                name: 'latitude',
                value: self.latitude()
            }));

            form.append($('<input/>', {
                type: 'hidden',
                name: 'longitude',
                value: self.longitude()
            }));

            form.append($('<input/>', {
                type: 'hidden',
                name: 'county',
                value: self.county()
            }));

            form.append($('<input/>', {
                type: 'hidden',
                name: 'state',
                value: self.state()
            }));

            form.appendTo('body').submit();
        }
        else {
            self.errors.showAllMessages();
        }
    }

    self.errors = ko.validation.group(self);
}

