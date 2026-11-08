var cancelButton = $("#CancelButton");
var withdrawDiv = $("#WithdrawDiv");
var withdrawAnchor = $("#WithdrawAnchor");
var viewReportAnchor = $("#ViewReportAnchor");
var emailHidden = $("#EmailHidden");
var okButton = $("#OKButton");


$(document).ready(function () {
    if (withdrawAnchor.length) {
        if (withdrawDiv) {
            withdrawDiv.position({
                my: "left top",
                at: "right top",
                of: withdrawAnchor,
                collision: "flip"
            });

            withdrawDiv.hide();
        };

        withdrawAnchor.click(function () {
            withdrawDiv.toggle(200);
        });
    };

    var url = viewReportAnchor.attr("data-url");
    if (url) {
        prepareEmail(url);
    };
});

okButton.click(function() {
    setEmailTemplate("");
});

cancelButton.click(function () {
    withdrawDiv.toggle(200);
});

function prepareEmail(url) {
    $.get(
        url,
        function (response) {
            setEmailTemplate(response);
        });
};

//Writes the email template into #EmailHidden and notifies the server of the new value.
//jQuery's .val() mutates the DOM property WITHOUT raising an "input" event, so the Blazor
//component that owns this element would never observe the write and would mail an empty
//body. Dispatching a bubbling native "input" event is what makes the component's
//@bind:event="oninput" pick the value up. The dispatch is deliberately done here, next to
//the only two writers, rather than read back from C# through script evaluation - which
//would break under a Content-Security-Policy that withholds 'unsafe-eval'.
function setEmailTemplate(value) {
    emailHidden.val(value);

    var element = emailHidden[0];

    if (element) {
        element.dispatchEvent(new Event("input", { bubbles: true }));
    }
};