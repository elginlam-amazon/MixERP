/*
    End-of-day operation controller for Modules/Finance/EODOperation.

    EXTRACTED VERBATIM (apart from the SignalR client, below) from the inline
    <script type="text/javascript"> block of Modules/Finance/EODOperation.ascx. It had to leave
    the markup: Blazor STRIPS <script> elements inside a .razor file, so the ported component
    loads this file instead (see the note left in EODOperation.ascx).

    SIGNALR: the three jQuery-SignalR proxy calls were replaced with the ASP.NET Core client.
    EVERY CALLBACK AND FUNCTION NAME IS PRESERVED, because they are a cross-file contract:

      | legacy                                            | now                                      |
      |---------------------------------------------------|------------------------------------------|
      | $.connection.dayOperationHub.client.getNotification = fn | dayOperationConnection.on("getNotification", fn) |
      | $.connection.dayOperationHub.server.performEOD()  | dayOperationConnection.invoke("PerformEOD") |
      | $.connection.baseHub.server.terminate(progress)   | updateHubConnection.invoke("Terminate", progress) |

    WHY TWO CONNECTIONS. "terminate" belongs to the host's BaseHub, which the host already maps
    at /hubs/update and publishes as window.updateHubConnection
    (wwwroot/js/mixerp/master-page/notification.js + eod.js). Reusing that one connection is what
    keeps the host's eod.js EOD modal and this page's progress bar in step - they are the same
    broadcast. The dayOperationHub is this module's OWN hub and needs its own route.

    HUB STATUS: Modules/Finance/Hubs/DayOperationHub.cs is now an ASP.NET Core
    Microsoft.AspNetCore.SignalR.Hub with the SAME "getNotification" client method and the SAME
    "PerformEOD" server method. The host must map it at the route below:
        app.MapFinanceHubs();   // MixERP.Net.Core.Modules.Finance.Hubs

    window.connectionListener and window.terminateListener are declared on WINDOW on purpose:
    the host's wwwroot/js/mixerp/master-page/eod.js calls both of them by bare global name.
*/
(function () {
    "use strict";

    //The route the ported DayOperationHub must be mapped to:
    //    app.MapHub<MixERP.Net.Core.Modules.Finance.Hubs.DayOperationHub>(DayOperationHubRoute)
    var DayOperationHubRoute = "/hubs/finance/dayoperation";

    var counter = 120;
    var interval;
    var url;
    var start;
    var receivingData = true;
    var toggle = false;
    var dayOperationConnection = null;

    var triggers = $("[data-popup]");

    triggers.each(function () {
        $(this).popup({
            popup: $(this).data("popup"),
            hoverable: true,
            position: 'bottom left',
            delay: {
                show: 300,
                hide: 800
            }
        });
    });

    function logConnectionError(error) {
        if (window.console) {
            console.error(error);
        }
    }

    //Replaces $.connection.dayOperationHub. Built once per page load, as the jQuery proxy was.
    function getDayOperationConnection() {
        if (dayOperationConnection) {
            return dayOperationConnection;
        }

        if (typeof signalR === "undefined") {
            logConnectionError("The SignalR client (/js/lib/signalr/signalr.js) is not loaded; the end-of-day hub is unreachable.");
            return null;
        }

        dayOperationConnection = new signalR.HubConnectionBuilder()
            .withUrl(DayOperationHubRoute)
            .withAutomaticReconnect()
            .build();

        //Server: Clients.Caller.getNotification(message, condition) - name preserved exactly.
        dayOperationConnection.on("getNotification", function (msg, condition) {
            AddItem(msg);

            if (condition.toLowerCase() === "error") {
                receivingData = false;

                $(".blue").removeClass("blue").addClass("red");
                $("h2").removeClass("blue").addClass("red");
                $(".loading").removeClass("loading");
                $(".progress").removeClass("success").addClass("error");
            };

            if (msg === "OK") {
                receivingData = false;
                $(".loading").removeClass("loading");
                notifyServer();
            };
        });

        dayOperationConnection.start().catch(logConnectionError);

        return dayOperationConnection;
    }

    $(function () {
        //EOD Notices from PostgreSQL Server
        getDayOperationConnection();
    });

    //Termination Signal. Called BY NAME from the host's /js/mixerp/master-page/eod.js.
    window.terminateListener = function (counter) {
        $("#PerformEODButton").addClass("disabled");
        $("#StartButton").addClass("disabled");
        $("#InitializeButton").addClass("disabled");

        $('.progress').progress({
            percent: counter
        });
    };

    //Connection to EOD SignalR Hub was successful.
    //Called BY NAME from the host's /js/mixerp/master-page/eod.js when it attaches.
    window.connectionListener = function () {
        $("#StartButton").removeClass("disabled loading");
        $("#OKButton").removeClass("disabled loading");

        $("#StartButton").click(function () {
            $(this).addClass("disabled");
            var ajaxInitializeEODOperation = intializeEODOperation();

            ajaxInitializeEODOperation.success(function (msg) {
                if (msg.d.toString() === "true") {
                    triggers.popup('hide');

                    start = new Date;

                    interval = setInterval(sendMessage, 5000);
                    return;
                };

                alert('Access is denied');
            });

            ajaxInitializeEODOperation.fail(function (xhr) {
                logAjaxErrorMessage(xhr);
            });
        });

        $("#OKButton").click(function () {
            $(this).addClass("disabled");
            triggers.popup('hide');
            $(".ui.blue.header").removeClass("initially hidden");
            ShowProgress(0);

            //Was: $.connection.dayOperationHub.server.performEOD();
            var connection = getDayOperationConnection();

            if (connection) {
                connection.invoke("PerformEOD").catch(logConnectionError);
            }
        });
    };

    function AddItem(msg) {
        var timestamp = new Date().toISOString();

        var html = "<div class='item'><i class='ui big blue loading settings icon'></i><div class='content'><div class='ui blue header'>{0}</div>{1}</div></div>";

        html = String.format(html, timestamp, msg);

        $(".ui.celled.list").append(html);
    };

    function ShowProgress(counter) {
        if (receivingData) {
            if (counter > 100 || counter <= 0) {
                if (toggle === true) {
                    toggle = false;
                } else {
                    toggle = true;
                }
            };

            if (toggle) {
                counter += 5;
            } else {
                counter -= 5;
            };

            $('.progress').progress({
                percent: counter,
                limitValues: true,
                label: "ratio"
            });

            setTimeout(ShowProgress, 100, counter);

        } else {
            $('.progress').progress({
                percent: 100,
                limitValues: true,
                label: "ratio"
            });
        };
    };

    function notifyServer() {
        var ajaxStartNewDay = startNewDay();

        ajaxStartNewDay.success(function () {
            //Okay
        });

        ajaxStartNewDay.fail(function (xhr) {
            logAjaxErrorMessage(xhr);
        });

    };

    function startNewDay() {
        //Was: /Modules/Finance/Services/EODOperation.asmx/StartNewDay. The ported endpoint
        //(Endpoints/FinanceEODOperationEndpoints) preserves the {"d": ...} ASMX envelope, so
        //ONLY the url changed - the response handling above is untouched.
        url = "/services/finance/eodoperation/startnewday";

        return getAjax(url, null);
    };

    function intializeEODOperation() {
        //Was: /Modules/Finance/Services/EODOperation.asmx/InitializeEODOperation - same
        //envelope, url only. (The misspelling of this function name is the original's and is
        //kept: it is referenced above.)
        url = "/services/finance/eodoperation/initializeeodoperation";

        return getAjax(url, null);
    };

    var sendMessage = function () {
        var elapsed = parseInt((new Date - start) / 1000);
        var progress = parseInt((elapsed / counter) * 100);

        if (progress > 100) {
            clearInterval(interval);
            return;
        };

        //Was: $.connection.baseHub.server.terminate(progress);
        //BaseHub.Terminate is served by the host's /hubs/update connection, which
        //notification.js publishes as window.updateHubConnection.
        if (window.updateHubConnection) {
            window.updateHubConnection.invoke("Terminate", progress).catch(logConnectionError);
        } else {
            logConnectionError("window.updateHubConnection is not available; the end-of-day terminate signal was not sent.");
        }
    };
})();
