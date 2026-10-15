/*
    Compatibility layer for the classic jQuery SignalR hub proxies ($.connection.<hub>.client/.server and
    $.connection.hub.start()) on top of the ASP.NET Core SignalR client (@microsoft/signalr).
    The hub list is provided by the server at "/signalr/hubs" (window.mixerpHubs) before this file runs.
*/
(function ($, signalR) {
    "use strict";

    if (!$ || !signalR || !window.mixerpHubs) {
        return;
    }

    var connection = {};
    var proxies = [];

    function createProxy(hub) {
        var proxy = {
            name: hub.name,
            client: {},
            server: {},
            connection: null,
            starting: null
        };

        proxy.ensureStarted = function () {
            if (proxy.starting) {
                return proxy.starting;
            }

            proxy.connection = new signalR.HubConnectionBuilder()
                .withUrl(hub.url)
                .withAutomaticReconnect()
                .build();

            // Client callbacks must be declared before start(), exactly as with the classic proxies.
            $.each(proxy.client, function (method, callback) {
                if (typeof callback === "function") {
                    proxy.connection.on(method, function () {
                        return callback.apply(proxy, arguments);
                    });
                }
            });

            proxy.starting = proxy.connection.start();
            return proxy.starting;
        };

        $.each(hub.methods, function (index, method) {
            proxy.server[method] = function () {
                var args = [method].concat(Array.prototype.slice.call(arguments));
                var deferred = $.Deferred();

                proxy.ensureStarted().then(function () {
                    return proxy.connection.invoke.apply(proxy.connection, args);
                }).then(function (result) {
                    deferred.resolve(result);
                }, function (error) {
                    deferred.reject(error);
                });

                return deferred.promise();
            };
        });

        return proxy;
    }

    $.each(window.mixerpHubs, function (index, hub) {
        var proxy = createProxy(hub);
        proxies.push(proxy);
        connection[hub.name] = proxy;
    });

    connection.hub = {
        start: function () {
            var deferred = $.Deferred();

            // Connect the hubs this page listens to (hubs with client callbacks); others connect on first server call.
            var starts = $.map(proxies, function (proxy) {
                var hasCallbacks = $.map(proxy.client, function (callback) {
                    return typeof callback === "function" ? true : null;
                }).length > 0;

                return hasCallbacks ? proxy.ensureStarted() : null;
            });

            Promise.all(starts).then(function () {
                deferred.resolve();
            }, function (error) {
                deferred.reject(error);
            });

            return deferred.promise();
        }
    };

    $.connection = connection;
})(window.jQuery, window.signalR);
