/*
    End-of-day (EOD) terminate listener.

    MIGRATED FROM: the inline <script> blocks at the bottom of MixERPMaster.Master and
    BackendMaster.Master. Blazor strips <script> elements inside .razor files, so the two blocks
    live here and the layouts invoke them through JS interop in OnAfterRenderAsync(firstRender).

    SIGNALR: the legacy code used the jQuery SignalR proxy
    ($.connection.baseHub.client.terminate / $.connection.hub.start()). The ASP.NET Core client
    has no generated proxy, so the "terminate" server call is received on the hub connection
    created by /js/mixerp/master-page/notification.js (window.updateHubConnection). If that
    connection does not exist yet this file builds its own against the same "/hubs/update" route.

    The localized strings are passed in from the layout (C# owns MixERP.Net.i18n.Resources).
*/
(function () {
    "use strict";

    function getConnection() {
        if (window.updateHubConnection) {
            return window.updateHubConnection;
        }

        if (typeof signalR === "undefined") {
            if (window.console) {
                console.error("The SignalR client (/js/lib/signalr/signalr.js) is not loaded; the end-of-day listener is inactive.");
            }
            return null;
        }

        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/update")
            .withAutomaticReconnect()
            .build();

        window.updateHubConnection = connection;

        connection.start().catch(function (error) {
            if (window.console) {
                console.error(error);
            }
        });

        return connection;
    }

    function attach(handler) {
        var connection = getConnection();

        if (!connection) {
            return;
        }

        //Guard against a second attach when the layout re-renders: the original attached exactly once
        //per page load.
        if (connection.__mixerpEodAttached) {
            return;
        }

        connection.__mixerpEodAttached = true;

        //Server: Clients.All.terminate(completed, catalog)
        connection.on("terminate", handler);

        if (typeof connectionListener === "function") {
            connectionListener();
        }
    }

    //MixERPMaster.Master behaviour: writes the notification badge and the notification item itself.
    window.mixerpInitializeEod = function (eodBegunMessage, signOutUrl) {
        var started = false;

        attach(function (c, cat) {
            if (top.catalog !== cat) {
                return;
            }

            if (typeof terminateListener === "function") {
                terminateListener(c);
            }

            $("#EODModal .circular.segment h2").html(c + "%");

            if (!started) {
                $("#NotificationMenu span").addClass("ui red label").html("1");
                $("#EODModal").modal("show");
                $("#Notification").append("<div class=\"item\" onclick=\"$('#EODModal').modal('show');\">" + eodBegunMessage + "</div>");
                started = true;
            }

            if (c === 100) {
                window.location = signOutUrl;
            }
        });
    };

    //BackendMaster.Master behaviour: delegates to addNotification instead of writing the badge.
    window.mixerpInitializeBackendEod = function (eodBegunMessage, signOutUrl) {
        var started = false;

        attach(function (c, cat) {
            if (top.catalog !== cat) {
                return;
            }

            if (typeof terminateListener === "function") {
                terminateListener(c);
            }

            $("#EODModal .circular.segment h2").html(c + "%");

            if (!started) {
                $("#EODModal").modal("show");
                addNotification(eodBegunMessage, "$('#EODModal').modal('show');");
                started = true;
            }

            if (c === 100) {
                window.location = signOutUrl;
            }
        });
    };

    //MixERPReportMaster.Master inline script, moved here verbatim.
    window.closeWindow = function () {
        parent.$.fn.colorbox.close();
    };
})();
