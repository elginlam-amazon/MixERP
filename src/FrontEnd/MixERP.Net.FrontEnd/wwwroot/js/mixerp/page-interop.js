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

//Page-level interop module for the converted .aspx pages (checklist tasks 83-89).
//
//WHY IT EXISTS. Each WebForms page carried its own <script src="..."> elements at the bottom of
//its own <body> (SignIn.aspx -> sha.js + Pages/SignIn.aspx.js, Install.aspx ->
//Pages/Install.aspx.js, Modules/FirstSteps.aspx -> underscore + Pages/Modules/Checklists.aspx.js,
//Modules/Update.aspx -> an inline block). A Blazor component may not contain a <script> element,
//and App.razor is shared by every page, so the per-page scripts are loaded from the page's
//OnAfterRenderAsync(firstRender) through this module instead. Loading is SEQUENTIAL because the
//original document order was load-bearing (jsSHA before SignIn.aspx.js).
//
//It is an ES module (imported through IJSRuntime) so no "eval" and no global namespace pollution
//is required, matching MixERP.Net.Common's mixerp-interop.js.

//Loads the given script urls in order, each at most once per document — the same "declared once in
//the page" semantics the original <script> elements had.
export async function loadScripts(urls) {
    if (!urls) {
        return;
    }

    for (const url of urls) {
        await loadScript(url);
    }
}

function loadScript(url) {
    return new Promise(function (resolve, reject) {
        if (!url) {
            resolve();
            return;
        }

        const existing = document.querySelector('script[data-mixerp-page-script="' + cssEscape(url) + '"]');

        if (existing) {
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = url;
        script.async = false;
        script.setAttribute('data-mixerp-page-script', url);
        script.onload = function () { resolve(); };
        script.onerror = function () { reject(new Error('Could not load ' + url)); };

        document.body.appendChild(script);
    });
}

//SignIn.aspx rendered <body id="SignInBody">, which is what the theme stylesheet
//(mixerp.min.css: ".theme, #SignInBody { background-color: #67238b }") paints purple. The body
//element now belongs to App.razor and is shared, so the page sets and clears the id itself.
export function setBodyId(id) {
    if (id) {
        document.body.id = id;
    }
}

export function clearBodyId(id) {
    if (id && document.body.id === id) {
        document.body.id = '';
    }
}

//Modules/Default.aspx honoured "plugin.RemoveTheme" with "Page.Theme = null", which stopped the
//WebForms theme engine from injecting the App_Themes stylesheet. That stylesheet is now a <link>
//in App.razor, so the page disables it instead.
export function setThemeEnabled(enabled) {
    const link = document.querySelector('link[href="/css/themes/mixerp/mixerp.min.css"]');

    if (link) {
        link.disabled = !enabled;
    }
}

//Angular wiring of Modules/FirstSteps.aspx.
//
//The original page declared <div data-ng-include="'/views/modules/Checklists.aspx.html'"> and
//loaded Pages/Modules/Checklists.aspx.js (which calls MixERPApp.controller(...)) at the bottom of
//the body, i.e. BEFORE the document-level data-ng-app bootstrap compiled the markup. In Blazor the
//page script can only be loaded after the first render, which is after that bootstrap, so a
//controller registered then would never be applied. The container is therefore rendered WITHOUT
//the data-ng-include attribute (the document bootstrap leaves it alone), and this function adds
//the attribute and bootstraps the MixERP angular module on that element once the controller script
//has been loaded. The module's registration queue — including the just-loaded controller — is
//processed by that bootstrap, which is what makes the checklist render.
export function bootstrapAngularInclude(elementId, includePath) {
    const element = document.getElementById(elementId);

    if (!element || !window.angular) {
        return false;
    }

    if (element.getAttribute('data-mixerp-ng-bootstrapped') === 'true') {
        return true;
    }

    element.setAttribute('data-ng-include', "'" + includePath + "'");
    element.setAttribute('data-mixerp-ng-bootstrapped', 'true');

    window.angular.bootstrap(element, ['MixERPApp']);

    return true;
}

function cssEscape(value) {
    return window.CSS && window.CSS.escape ? window.CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
}

//The OnClientClick gate of a converted asp:Button whose client script was
//"return $('.form').form('validate form');" (Site/Account/ChangePassword.aspx, checklist task 98).
//WebForms suppressed the postback when that expression returned false; the migrated page calls
//this function FIRST and returns early when it is false, which is the same gate. A missing
//jQuery/Semantic-UI form plugin returns true so the server-side validation (which is the
//authoritative one) still runs, exactly as a page without the client script behaved.
export function validateForm(selector) {
    const target = window.jQuery ? window.jQuery(selector || '.form') : null;

    if (!target || !target.length || typeof target.form !== 'function') {
        return true;
    }

    return target.form('validate form') !== false;
}
