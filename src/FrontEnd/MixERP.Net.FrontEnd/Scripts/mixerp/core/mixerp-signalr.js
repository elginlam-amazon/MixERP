/*
 * MixERP SignalR client bridge.
 *
 * The server moved from ASP.NET SignalR 2 (OWIN, "/signalr/hubs" generated proxies) to ASP.NET Core SignalR,
 * where every hub is mapped at "/signalr/{hubName}". This script keeps the subset of the SignalR 2 jQuery API
 * used by MixERP pages working on top of the @microsoft/signalr client (signalr.min.js must be loaded first):
 *
 *   $.connection.someHub.client.someMethod = function (...) { };   // server-to-client callback
 *   $.connection.someHub.server.someMethod(arg1, arg2);            // client-to-server invocation (returns a promise)
 *   $.connection.hub.start().done(function () { });                // starts every hub used by the page
 */
(function ($, signalR) {
    "use strict";

    if (!$ || !signalR) {
        return;
    }

    var hubs = {};

    function createHub(name) {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/signalr/" + name)
            .withAutomaticReconnect()
            .build();

        var client = {};
        var subscribed = {};

        var hub = {
            connection: connection,
            client: client,
            server: new Proxy({}, {
                get: function (target, method) {
                    return function () {
                        var args = Array.prototype.slice.call(arguments);
                        return connection.invoke.apply(connection, [method].concat(args));
                    };
                }
            }),
            subscribe: function () {
                Object.keys(client).forEach(function (method) {
                    if (subscribed[method]) {
                        return;
                    }

                    subscribed[method] = true;

                    connection.on(method, function () {
                        var handler = client[method];

                        if (typeof handler === "function") {
                            handler.apply(hub, arguments);
                        }
                    });
                });
            }
        };

        return hub;
    }

    var hubConnection = {
        start: function () {
            var deferred = $.Deferred();

            var starts = Object.keys(hubs).map(function (name) {
                var hub = hubs[name];
                hub.subscribe();
                return hub.connection.start();
            });

            Promise.all(starts).then(function () {
                deferred.resolve();
            }, function (error) {
                if (window.console) {
                    window.console.error(error);
                }

                deferred.reject(error);
            });

            return deferred.promise();
        }
    };

    $.connection = new Proxy({}, {
        get: function (target, name) {
            if (name === "hub") {
                return hubConnection;
            }

            if (typeof name !== "string") {
                return undefined;
            }

            if (!hubs[name]) {
                hubs[name] = createHub(name);
            }

            return hubs[name];
        }
    });
})(window.jQuery, window.signalR);
