/*
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
*/

//Port of the inline BottomScriptContentPlaceholder block of Modules/Update.aspx
//(checklist task 89). A .razor file may not contain a <script> element, so the block lives here
//and Components/Pages/Modules/Update.razor loads it from OnAfterRenderAsync(firstRender).
//
//SIGNALR: "$.connection.updateHub.client.updateInstallationNotification = ..." became
//connection.on("updateInstallationNotification", ...) and
//"$.connection.updateHub.server.installUpdates()" became connection.invoke("InstallUpdates").
//The connection itself is the single /hubs/update connection created by
//mixerp/master-page/notification.js and published as window.updateHubConnection; notification.js
//forwards the "updateInstallationNotification" message to window.updateInstallationNotification
//when a page defines it, which is this page.

(function () {
    //The original wrote "var console = $('#Console');" at page scope. Kept function-scoped here so
    //the page cannot shadow window.console for the rest of the application.
    var consoleElement = $("#Console");
    var updateButton = $("#UpdateButton");

    function disableUpdateButton() {
        $("#UpdateButton").addClass("disabled loading");
    };

    function addToConsole(timestamp, label, message) {
        var item = $("<div class='item'/>");
        var icon = $("<i class='ui big settings blue icon'></i>");
        item.append(icon);

        var content = $("<div class='content'/>");

        var labelContainer = $("<div class='ui grey header' />");
        labelContainer.html(timestamp);
        content.append(labelContainer);

        var span = $("<strong/>");
        span.html(label + " : ");
        content.append(span);

        var messageContainer = $("<span />");
        messageContainer.html(message);
        content.append(messageContainer);

        item.append(content);

        consoleElement.append(item);

        var height = consoleElement[0].scrollHeight;
        consoleElement.scrollTop(height);
    };

    //Server: Clients.All.updateInstallationNotification(timestamp, description, message).
    //notification.js calls this hook for the /hubs/update message.
    window.updateInstallationNotification = function (timestamp, label, message) {
        disableUpdateButton();
        addToConsole(timestamp, label, message);
    };

    //The original connectionListener(): enable the button once the hub connection is up and wire
    //the click to the hub's InstallUpdates method.
    window.connectionListener = function () {
        updateButton = $("#UpdateButton");
        updateButton.removeClass("disabled loading");

        updateButton.off("click.mixerpUpdate").on("click.mixerpUpdate", function () {
            if (!confirmAction()) {
                return;
            };

            disableUpdateButton();

            var connection = window.updateHubConnection;

            if (!connection) {
                return;
            };

            connection.invoke("InstallUpdates").catch(function (error) {
                if (window.console) {
                    window.console.error(error);
                };
            });
        });
    };

    $(function () {
        //eod.js calls connectionListener() when it attaches to the hub connection. When the
        //connection already exists by the time this script loads (the layout renders before the
        //page's scripts are fetched), enable the button straight away.
        if (window.updateHubConnection) {
            window.connectionListener();
        };
    });
})();
