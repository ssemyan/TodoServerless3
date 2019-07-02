AZ.Documents = (function (ko) {
    "use strict";

    var docViewModel = {
        documents: ko.observableArray(),
        currentUser: ko.observable(),
        apiUser: ko.observable(),
        showLogout: ko.observable()
    };

    function getApiData() {

        // Get list of Documents for the current user (as determined by the API)
        if (!noApi) {
            AZ.Ajax.MakeAjaxCall("GET",
                apiUrl,
                null,
                function (data) {
                    docViewModel.documents(data.Items);
                    docViewModel.apiUser(data.UserName);
                    ko.applyBindings(docViewModel);
                });
        } else {
            docViewModel.apiUser("No Api User");
            ko.applyBindings(docViewModel);
        }
    }

    // Do this on start
    $(document).ready(function () {

        // Determine if we are calling the API and/or doing auth
        if (noLogin) {
            docViewModel.currentUser("No Login User");
            getApiData();
        } else {
            // Determine if we are logged in or not
            var user = AZ.Ajax.CurrentUser();
            if (user) {
                docViewModel.currentUser(user.userName);
                docViewModel.showLogout(true);
                getApiData();
            } else {
                ko.applyBindings(docViewModel);
            }
        }
        
    });

    return {
        model: docViewModel,

        AddEdit: function () {
            var newTodo = prompt("Enter new todo item:");
            if (newTodo) {
                var newTodoItem = { ItemName: newTodo };
                if (noApi) {
                    newTodoItem.ItemCreateDate = new Date();
                    docViewModel.documents.push(newTodoItem);
                } else {
                    AZ.Ajax.MakeAjaxCall("POST",
                        apiUrl,
                        JSON.stringify(newTodoItem),
                        function (data) {
                            docViewModel.documents.push(data);
                        });
                }
            }
        },

        Remove: function (item) {
            if (confirm("Mark item '" + item.ItemName + "' as completed?")) {
                if (noApi) {
                    docViewModel.documents.remove(item);
                } else {
                    AZ.Ajax.MakeAjaxCall("DELETE",
                        apiUrl + "/" + item.id,
                        null,
                        function (data) {
                            docViewModel.documents.remove(item);
                        });
                }
            }
        }
    };
}(ko));