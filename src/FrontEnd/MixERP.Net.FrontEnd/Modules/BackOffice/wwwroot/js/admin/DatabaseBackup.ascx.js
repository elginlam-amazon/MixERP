/*
    Database-backup controller for Modules/BackOffice/Admin/DatabaseBackup.

    SIGNALR MIGRATION: the four jQuery-SignalR proxy calls were replaced with the ASP.NET Core
    client. EVERY CALLBACK NAME IS PRESERVED - they are the server contract:

      | legacy                                               | now                                     |
      |------------------------------------------------------|-----------------------------------------|
      | $.connection.dbHub.server.backupDatabase(name)       | dbConnection.invoke("BackupDatabase", name) |
      | $.connection.dbHub.client.getNotification = fn       | dbConnection.on("getNotification", fn)  |
      | $.connection.dbHub.client.backupCompleted = fn       | dbConnection.on("backupCompleted", fn)  |
      | $.connection.dbHub.client.backupFailed = fn          | dbConnection.on("backupFailed", fn)     |

    Everything else in this file is unchanged, including the `connectionListener` global (the
    host's /js/mixerp/master-page/eod.js calls it by bare name) and the deliberate
    `isValidFileName` wrapper.

    ⚠ OWED BY THE PORTING SPECIALISTS: Modules/BackOffice/Hubs/DbHub.cs still derives from
    Microsoft.AspNet.SignalR.Hub and is NOT mapped. Until it is ported to
    Microsoft.AspNetCore.SignalR.Hub and mapped at the route below, the Backup button cannot
    invoke anything. The hub itself also still uses System.Web.Hosting.HostingEnvironment.MapPath
    and a Windows pg_dump.exe .bat file, both of which need real replacements.
*/

//The route the ported DbHub must be mapped to:
//    app.MapHub<MixERP.Net.Core.Modules.BackOffice.Hubs.DbHub>(dbHubRoute)
var dbHubRoute = "/hubs/backoffice/db";

var list = $(".ui.celled.list");
var messages = $("#Messages");
var backupButton = $("#BackupButton");
var counter = 0;
var header = $(".ui.blue.header");
var backupNameInputText = $("#BackupNameInputText");
var dbConnection = null;


function AddItem(msg, cls) {
    if (!cls) {
        cls = "blue";
    };

    var html = "<div class='item'><i class='ui {0} loading recycle icon'></i><div class='content'>{1}. {2} : {3}</div></div>";
    counter++;

    var timestamp = new Date().toISOString();
    html = String.format(html, cls, counter, timestamp, msg);

    list.append(html);
    messages.scrollTop(messages.prop("scrollHeight"));
};

function InitializeMessage() {
    list.html("");
    counter = 0;
    messages.css("height", $(document).height() - 350 + "px").addClass("ui segment");
}

function logDbHubError(error) {
    if (window.console) {
        console.error(error);
    }
}

//Replaces the $.connection.dbHub proxy. Built once per page load, as the jQuery proxy was.
function getDbConnection() {
    if (dbConnection) {
        return dbConnection;
    }

    if (typeof signalR === "undefined") {
        logDbHubError("The SignalR client (/js/lib/signalr/signalr.js) is not loaded; the database-backup hub is unreachable.");
        return null;
    }

    dbConnection = new signalR.HubConnectionBuilder()
        .withUrl(dbHubRoute)
        .withAutomaticReconnect()
        .build();

    //Server: Clients.Caller.getNotification(message)
    dbConnection.on("getNotification", function (msg) {
        AddItem(msg);
    });

    //Server: Clients.Caller.backupCompleted(message)
    dbConnection.on("backupCompleted", function (msg) {
        backupButton.removeClass("disabled loading");
        AddItem(msg);
    });

    //Server: Clients.Caller.backupFailed(message)
    dbConnection.on("backupFailed", function (msg) {
        backupButton.removeClass("disabled loading");
        AddItem(msg, "red");
    });

    dbConnection.start().catch(logDbHubError);

    return dbConnection;
}


//Connection to EOD SignalR Hub was successful.
function connectionListener() {
    backupButton.removeClass("disabled loading");

    backupButton.click(function () {
        var backupName = backupNameInputText.val();

        if (!isValidFileName(backupName)) {
            makeDirty(backupNameInputText);
            return;
        };


        removeDirty(backupNameInputText);

        $(this).addClass("disabled loading");
        header.removeClass("initially hidden");
        InitializeMessage();

        //Was: $.connection.dbHub.server.backupDatabase(backupName);
        var connection = getDbConnection();

        if (connection) {
            connection.invoke("BackupDatabase", backupName).catch(logDbHubError);
        }

    });
};

function isValidFileName(fileName) {
    if (isNullOrWhiteSpace(fileName)) {
        return false;
    };

    return true;
};

$(function () {
    //The three client handlers are registered inside getDbConnection(), so that they are
    //attached to the connection before it starts - the ASP.NET Core client requires handlers to
    //be registered before start(), unlike the jQuery proxy which allowed either order.
    getDbConnection();
});
