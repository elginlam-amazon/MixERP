$(document).ready(function () {
    if (update === "1") {
        addNotification(Resources.Titles.Update(), "document.location = \"/Modules/Update\";");
    };

    if (firstStepsPending.toUpperCase().substring(0, 1) === "T") {
        addNotification(Resources.Titles.FirstSteps(), "document.location = \"/Modules/FirstSteps\";");
    };

    //ASP.NET Core SignalR replacement of the former "$.connection.updateHub" jQuery SignalR proxy.
    //Requires /js/lib/signalr/signalr.min.js (the @microsoft/signalr browser bundle).
    if (typeof signalR !== "undefined") {
        var updateConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/update")
            .withAutomaticReconnect()
            .build();

        //Server: Clients.Caller.getNotification(message)
        updateConnection.on("getNotification", function (message) {
            addNotification(message, "");
        });

        //Server: Clients.All.updateInstallationNotification(timestamp, description, message)
        updateConnection.on("updateInstallationNotification", function (timestamp, label, message) {
            if (typeof window.updateInstallationNotification === "function") {
                window.updateInstallationNotification(timestamp, label, message);
            } else {
                addNotification(message, "");
            };
        });

        //Exposed so that the Update page can invoke "installUpdates" on the same connection.
        window.updateHubConnection = updateConnection;

        updateConnection.start().catch(function (error) {
            if (window.console) {
                console.error(error);
            };
        });
    };
});
