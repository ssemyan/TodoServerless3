// Global Namespace
var AZ = {};

AZ.Ajax = (function () {
    "use strict";

    var authContext = {};

    $(document).ready(function () {
        // Add please wait to body and attach to ajax function
        var loadingDiv = "<div id='ajax_loader' style='width: 100%;height: 100%;top: 0;left: 0;position: fixed;opacity: 0.7;background-color: #fff;z-index: 9999;text-align: center;display: none;'><h1 style='margin-top: 300px;'>Loading...</h1></div>";
        $("body").append(loadingDiv);

        $(document).ajaxStart(function () {
            $("#ajax_loader").show();
        });

        $(document).ajaxComplete(function (event, jqxhr, settings) {
            if (settings.url.startsWith("/.auth/")) return; // Keep loading div open on auth requests
            $("#ajax_loader").hide();
        });

        // Set up auth
        window.config = {
            instance: 'https://login.microsoftonline.com/',
            tenant: '72f988bf-86f1-41af-91ab-2d7cd011db47',
            clientId: '2d30de6d-ff51-4e69-9252-ae08f8d8a2b1'
        };
        authContext = new AuthenticationContext(config);

        // On check for error on callback then handle
        var loginError = authContext.getLoginError();
        if (loginError) {
            alert(loginError);
        } else {
            authContext.handleWindowCallback();
        }
    });

    // Errors just get an alert
    function handleBasicError(xhr, status, error) {
        alert("Error: " + error);
    }

    // Ajax call
    function makeAjaxCall(ajaxType, ajaxUrl, data, successFunc, headers) {

        // Get TodoList Data
        $.ajax({
            type: ajaxType,
            url: ajaxUrl,
            data: data,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: successFunc,
            error: handleBasicError,
            headers: headers
        });
    }

    return {

        MakeAjaxCall: function (ajaxType, ajaxUrl, data, successFunc) {

            if (noLogin) {

                makeAjaxCall(ajaxType, ajaxUrl, data, successFunc);
            } else {

                // Acquire Token for Backend
                authContext.acquireToken(authContext.config.clientId, function (error, token) {

                    // Handle ADAL Error
                    if (error || !token) {
                        alert('ADAL Error Occurred: ' + error);
                        return;
                    }

                    var headers = {
                        'Authorization': 'Bearer ' + token
                    };

                    makeAjaxCall(ajaxType, ajaxUrl, data, successFunc, headers);
                });
            }
        },

        DoLogin: function () {
            authContext.login();
        },

        DoLogout: function () {
            authContext.logOut();
        },

        CurrentUser: function () {
            return authContext.getCachedUser();
        }
    };
}());
