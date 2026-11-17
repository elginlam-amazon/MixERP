/*
    Hub proxies for the ASP.NET Core SignalR JavaScript client (Scripts/microsoft-signalr/signalr.min.js).

    The pages were written against the SignalR 2 jQuery client, which exposed every hub as
    $.connection.<hubName> with a "client" object (callbacks the server invokes) and a "server"
    object (hub methods the page invokes), and opened them with $.connection.hub.start().
    This file keeps that surface on top of one HubConnection per hub, mapped on the server at /hubs/<hubName>.
*/
(function ($, signalR) {
    "use strict";

    var hubNames = ["baseHub", "updateHub", "dbHub", "dayOperationHub"];
    var connection = {};

    function createProxy(name) {
        var hubConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/" + name)
            .withAutomaticReconnect()
            .build();

        var proxy = { connection: hubConnection, started: null, handlers: {} };

        proxy.start = function () {
            if (!proxy.started) {
                proxy.started = hubConnection.start();
            };

            return proxy.started;
        };

        // Assigning proxy.client.someMethod registers the callback for server calls to "someMethod".
        proxy.client = new Proxy(proxy.handlers, {
            set: function (target, method, callback) {
                target[method] = callback;
                hubConnection.off(method);
                hubConnection.on(method, function () {
                    target[method].apply(proxy, arguments);
                });

                return true;
            }
        });

        // proxy.server.someMethod(args) invokes the hub method, opening the connection first if needed.
        proxy.server = new Proxy({}, {
            get: function (target, method) {
                return function () {
                    var args = [method].concat(Array.prototype.slice.call(arguments));

                    return proxy.start().then(function () {
                        return hubConnection.invoke.apply(hubConnection, args);
                    });
                };
            }
        });

        return proxy;
    };

    $.each(hubNames, function (i, name) {
        connection[name] = createProxy(name);
    });

    connection.hub = {
        start: function () {
            var deferred = $.Deferred();

            // Like SignalR 2, only hubs with client callbacks are connected up front.
            var pending = $.map(hubNames, function (name) {
                var proxy = connection[name];
                return Object.keys(proxy.handlers).length ? proxy.start() : null;
            });

            Promise.all(pending).then(function () {
                deferred.resolve();
            }, function (error) {
                deferred.reject(error);
            });

            return deferred.promise();
        }
    };

    $.connection = connection;
})(jQuery, signalR);
