/********************************************************************************
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
***********************************************************************************/

/*global allowedExtensions, appendParameter, Resources, getAjax, getData,
         undoUploadServiceUrl, uploadHandlerUrl, uploadButtonCallback */

/*
    BLAZOR MIGRATION NOTE

    This script used to cache #UndoButton / #UploadButton / #UploadedFilesHidden /
    #WarningLabel into module-level variables and bind their click handlers at top-level
    execution, plus wire .browse at $(window).load and input:file at $(function(){}).
    Under WebForms that was safe: a full page load always put the control's markup in the
    DOM before the script ran, and every re-render was another full page load.

    Under Blazor the script element is injected once per circuit (JSUtility.AddJSReference
    is keyed), it can be appended BEFORE the component markup reaches the browser, and
    SPA navigation re-renders the component without re-running the script. Top-level
    binding therefore attaches to nothing, and the control renders but silently does
    nothing.

    All wiring now lives in window.initAttachment(), which Attachment.razor calls from
    OnAfterRenderAsync(firstRender) for EVERY component instance. The element lookups are
    re-done on each call (never cached across calls), and every binding uses .off().on()
    so a repeat call rebinds rather than stacking duplicate handlers - initAttachment() is
    idempotent. The behaviour of each handler is unchanged from the original.
*/

//Re-queried on every initAttachment() call rather than cached at load, so a re-rendered
//component instance (which brings brand new DOM nodes) is wired to its own elements.
function getUploadedFilesHidden() {
    return $("#UploadedFilesHidden");
}

function getWarningLabel() {
    return $("#WarningLabel");
}

window.initAttachment = function () {
    var fileUploads = $("#FileUploads");

    //The component is not in the DOM yet (the script was injected before the markup
    //reached the browser) - the self-initialisation at the bottom of this file, or the
    //next component instance, will wire it up.
    if (!fileUploads.length) {
        return;
    }

    //Port of the former $(window).load handler.
    $(".browse").off("click.attachment").on("click.attachment", function () {
        var counter = this.id.replace("BrowseButton", "");
        var targetControlSelector = "#FileUpload" + counter;

        $(targetControlSelector).click();
        return false;
    });

    //Port of the former $(function(){ $("input:file").change(...) }) handler.
    $("input:file").off("change.attachment").on("change.attachment", function () {
        var counter = this.id.replace("FileUpload", "");
        var filePathSelector = "#FilePath" + counter;
        var fileName = $(this).val();

        if (fileName) {
            if (validateFileName(fileName)) {
                $(filePathSelector).html(fileName);
                $(filePathSelector).removeClass("big error");
                return;
            };

            $(filePathSelector).html(Resources.Messages.InvalidFile() + " " + fileName);
            $(filePathSelector).addClass("big error");
            $(this).val("");
        };
    });

    //Port of the former top-level undoButton.on("click", ...) binding.
    $("#UndoButton").off("click.attachment").on("click.attachment", function () {
        var uploadedFilesHidden = getUploadedFilesHidden();

        $(".browse").prop('disabled', false);
        if (uploadedFilesHidden.val() !== "") {
            if (confirm(Resources.Questions.AreYouSure())) {
                $.post(undoUploadServiceUrl, {
                    uploadedFilesJson: uploadedFilesHidden.val()
                }, function (data) {
                    var result = $(data).find("boolean").text();

                    if (result) {
                        resetAttachmentForm();
                        $.notify(Resources.Messages.UploadFilesDeleted(), "success");
                    };
                });
            };
        };
    });

    //Port of the former top-level uploadButton.on("click", ...) binding.
    $("#UploadButton").off("click.attachment").on("click.attachment", function () {
        if (hasFile()) {
            if (validateDuplicates()) {
                if (confirm(Resources.Questions.AreYouSure())) {
                    var uploads = [];

                    $(".upload").each(function () {
                        var fileUploadButtonId = $(this).attr("id");
                        var counter = fileUploadButtonId.replace("FileUpload", "");
                        var browseButtonSelector = "#BrowseButton" + counter;
                        var commentTextBoxSelector = "#CommentTextBox" + counter;
                        var progressBarSelector = "#Progress" + counter;
                        var originalFileName = $(this).val().replace(/^.*[\\\/]/, "");


                        var comment = $(commentTextBoxSelector).val();

                        $(this).upload(uploadHandlerUrl, function (success) {
                            $(browseButtonSelector).prop("disabled", true);
                            $("#" + fileUploadButtonId).val(""); //Clear file upload control.
                            var upload = new Upload(comment, success, originalFileName);
                            uploads.push(upload);
                            getUploadedFilesHidden().val(JSON.stringify(uploads));
                        }, function (progress, value) {
                            $(progressBarSelector).val(value);
                        });
                    }).promise().done(function () {
                        if (typeof uploadButtonCallback == "function") {
                            uploadButtonCallback();
                        };
                    });;

                    $("#FileUploads table *").disable();
                }
            }
        }
    });
};

function validateFileName(fileName) {
    var ext = fileName.split('.').pop();

    var index = $.inArray(ext, allowedExtensions);
    return index > -1;
};

$.fn.disable = function () {
    return this.each(function () {
        if (typeof this.disabled !== "undefined") this.disabled = true;
    });
};

$.fn.enable = function () {
    return this.each(function () {
        if (typeof this.disabled !== "undefined") this.disabled = false;
    });
};


function resetAttachmentForm() {
    var paragraphs = $(".path");
    var progressBars = $("progress");
    var comments = $(".comment");

    progressBars.val(0);
    paragraphs.html("");
    comments.val("");

    $("#FileUploads table *").enable();
    getUploadedFilesHidden().val("");
};

function Upload(comment, filePath, originalFileName) {
    this.Comment = comment;
    this.FilePath = filePath;
    this.OriginalFileName = originalFileName;
};


var hasFile = function () {
    var files = [];
    var filePath;

    $(".upload").each(function () {
        filePath = $(this).val();
        if (filePath) {
            files.push(filePath);
        };
    });

    return files.length > 0;
};


var validateDuplicates = function () {
    var files = [];
    var filePath;

    $(".upload").each(function () {
        filePath = $(this).val();
        if (filePath) {
            files.push(filePath);
        };
    });


    var sortedFiles = files.sort();
    var duplicates = [];

    for (var i = 0; i < files.length - 1; i++) {
        if (sortedFiles[i + 1] === sortedFiles[i]) {
            duplicates.push(sortedFiles[i]);
        };
    };

    var warningLabel = getWarningLabel();

    if (isNullOrWhiteSpace(duplicates)) { //No duplicate found.
        warningLabel.html("");
        return true;
    } else {
        warningLabel.html(Resources.Warnings.DuplicateFiles() + " : " + duplicates.join());
        return false;
    }
};

//Self-initialisation. This replaces the original's top-level binding plus its
//$(window).load / $(function(){}) handlers: when the <script> element is appended the
//component markup is normally already in the DOM, so the control is wired exactly as it
//was under WebForms without waiting for a round trip. Repeat instances are wired by
//Attachment.razor calling initAttachment() from OnAfterRenderAsync; the namespaced
//.off().on() bindings above make the two paths safe together.
if (window.jQuery) {
    $(function () {
        window.initAttachment();
    });
}
