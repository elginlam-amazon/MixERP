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

//Interop module for the report command panel. It replaces the WebForms
//ImageButton.OnClientClick strings of Report/CommandPanel.cs one for one.
//Loaded as an ES module so that no "eval" is needed: a Content-Security-Policy
//without 'unsafe-eval' would otherwise silently disable the command panel.

//Was: OnClientClick = "window.scrollTo(0,document.body.scrollHeight);"
export function scrollToBottom() {
    window.scrollTo(0, document.body.scrollHeight);
}

//Was: OnClientClick = "$('.report-parameter').toggle(500);return false;"
//The ".report-parameter" selector is a load-bearing contract: the panel it
//toggles is owned by FrontEnd/Reports/ReportViewer.aspx (line 26). Do NOT rename it.
//jQuery is supplied by the host page; without it the animated toggle is emulated
//so the filter button still opens the parameter form.
export function toggleReportParameter() {
    if (window.jQuery) {
        window.jQuery('.report-parameter').toggle(500);
        return;
    }

    const panels = document.querySelectorAll('.report-parameter');

    for (let i = 0; i < panels.length; i++) {
        const panel = panels[i];
        const hidden = window.getComputedStyle(panel).display === 'none';
        panel.style.display = hidden ? '' : 'none';
    }
}

//Was: OnClientClick = "closeWindow();"
//"closeWindow" is a page-supplied global (ScrudFactory/Scrud.js,
//ScrudItemSelector.js). The call is guarded: a page that does not define it must
//not raise an exception over the circuit.
export function closeWindow() {
    if (typeof window.closeWindow === 'function') {
        window.closeWindow();
    }
}
