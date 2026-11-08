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

//Interop module replacing the WebForms ClientScriptManager registration APIs.
//Loaded as an ES module so that no "eval" is required: a Content-Security-Policy
//without 'unsafe-eval' would otherwise silently kill every PageUtility.RegisterJavascript,
//JSUtility.AddJSReference and jQueryUI.AddjQueryUIDatePicker call.

//Runs a block of javascript by appending it as a real <script> element, keyed so that
//repeated registrations of the same key are applied only once. Replaces
//ClientScriptManager.RegisterStartupScript / RegisterClientScriptBlock with addScriptTags = true.
export function runScript(key, text) {
    if (!text) {
        return;
    }

    if (key && document.querySelector('[data-mixerp-script="' + cssEscape(key) + '"]')) {
        return;
    }

    const script = document.createElement('script');
    script.type = 'text/javascript';

    if (key) {
        script.setAttribute('data-mixerp-script', key);
    }

    script.text = text;
    document.head.appendChild(script);
}

//Injects a complete <script ...></script> markup fragment into the document head,
//preserving its attributes (notably src). Replaces the addScriptTags = false path.
export function appendScript(key, html) {
    if (!html) {
        return;
    }

    if (key && document.querySelector('[data-mixerp-script="' + cssEscape(key) + '"]')) {
        return;
    }

    const container = document.createElement('div');
    container.innerHTML = html;

    const source = container.firstElementChild;

    if (!source) {
        return;
    }

    const script = document.createElement('script');

    for (let i = 0; i < source.attributes.length; i++) {
        script.setAttribute(source.attributes[i].name, source.attributes[i].value);
    }

    if (key) {
        script.setAttribute('data-mixerp-script', key);
    }

    script.text = source.textContent;
    document.head.appendChild(script);
}

function cssEscape(value) {
    return window.CSS && window.CSS.escape ? window.CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
}
