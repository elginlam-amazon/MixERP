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
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
    Hub proxies for ASP.NET Core SignalR (@microsoft/signalr, Scripts/signalr/signalr.min.js).

    ASP.NET SignalR 2 generated "/signalr/hubs", which exposed every hub as
    $.connection.<hubName>.client / .server and started them with $.connection.hub.start().
    The ASP.NET Core SignalR client does not generate proxies and uses one connection per hub,
    so this file recreates that surface on top of the new client. Each hub is mapped on the
    server at "/signalr/<hubName>".
*/
(function ($, signalR) {
    if (!$ || !signalR) {
        return;
    };

    var hubs = {
        baseHub: ["terminate"],
        updateHub: ["installUpdates"],
        dayOperationHub: ["performEOD"],
        dbHub: ["backupDatabase"]
    };

    var createHub = function (name, serverMethods) {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/signalr/" + name)
            .withAutomaticReconnect()
            .build();

        var proxy = {
            connection: connection,
            client: {},
            server: {}
        };

        $.each(serverMethods, function (i, method) {
            proxy.server[method] = function () {
                var args = [method].concat(Array.prototype.slice.call(arguments));
                return connection.invoke.apply(connection, args);
            };
        });

        return proxy;
    };

    var proxies = {};

    $.each(hubs, function (name, serverMethods) {
        proxies[name] = createHub(name, serverMethods);
    });

    var start = function () {
        var deferred = $.Deferred();
        var pending = [];

        $.each(proxies, function (name, proxy) {
            //Client callbacks are assigned by the pages before start() is called.
            $.each(proxy.client, function (method, callback) {
                if (typeof callback === "function") {
                    proxy.connection.off(method);
                    proxy.connection.on(method, callback);
                };
            });

            //SignalR 2 multiplexed every hub over one connection. Here each hub has its own connection,
            //so only the shared base hub and the hubs the page subscribes to are opened.
            if (name === "baseHub" || !$.isEmptyObject(proxy.client)) {
                pending.push(proxy.connection.start());
            };
        });

        Promise.all(pending).then(function () {
            deferred.resolve();
        }, function (error) {
            deferred.reject(error);
        });

        return deferred.promise();
    };

    $.connection = $.extend(proxies, {
        hub: {
            start: start
        }
    });
})(window.jQuery, window.signalR);
