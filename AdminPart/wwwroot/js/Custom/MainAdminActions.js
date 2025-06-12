var selectedRefereeId = null; // Variable for keeping the selected referee
var selectedNameSurname = null;
let searchTimeout;

$(function () {
    $("#customAlert .btn-close").on("click", function (e) {
        e.preventDefault();
        $("#customAlert").addClass("d-none").removeClass("show");
        return false;
    });
    // declaring the format of input date & time field for filtrating matches
    flatpickr("#filterStartDateTime", {
            enableTime: true,        // Enable time selection
            mode: "range",           // Enable range selection (start and end)
            wrap: true,              // Wrap input for custom behavior
            altInput: true,          // Use a custom input field instead of a standard one
            dateFormat: "Y-m-d H:i", // Date and time format
            time_24hr: true,         // Use 24-hour time format
            minuteIncrement: 1,      // Increment minutes by 1
            plugins: [new rangePlugin({ input: "#filterEndDateTime" })], // Adding the rangePlugin
    });


});

$(document).on("click", "#logOutTheUser", function () {
	 logout();
});
$(document).on("click", ".referee-button", function () {
    $('.referee-button').removeClass('selected');

    $(this).addClass('selected');

    selectedRefereeId = $(this).data('id');
    selectedNameSurname = $(this).text();
});
$(document).on("click", "#showExcusesBtn", function (e) {
    e.preventDefault();

    $("#excusesModal").modal("show");
    $("#excusesModalBody").html('<div class="text-center"><span class="spinner-border"></span></div>');

    $.get("Admin/Referee/GetExcuses", function (data) {
        $("#excusesModalBody").html(data);
    }).fail(function () {
        $("#excusesModalBody").html("<div class='alert alert-danger'>Nepodařilo se načíst omluvy.</div>");
    });
});
$(document).on("click", "#showPreviousMatchesBtn", function (e) {
    e.preventDefault();

    $("#previousMatchesModal").modal("show");
    $("#previousMatchesModalBody").html('<div class="text-center"><span class="spinner-border"></span></div>');

    $.get("Admin/Match/GetPreviousMatches", function (data) {
        $("#previousMatchesModalBody").html(data);
    }).fail(function () {
        $("#previousMatchesModalBody").html("<div class='alert alert-danger'>Nepodařilo se načíst predchodzí zápasy.</div>");
    });
});
$(document).on("click", "#showFootballFieldsBtn", function (e) {
    e.preventDefault();

    $("#footbalfieldsModal").modal("show");
    $("#footbalfieldsModalBody").html('<div class="text-center"><span class="spinner-border"></span></div>');

    $.get("Admin/Field/GetPreviewOfFields", function (data) {
        $("#footbalfieldsModalBody").html(data);
    }).fail(function () {
        $("#footbalfieldsModalBody").html("<div class='alert alert-danger'>Nepodařilo se načíst přehled hřišť.</div>");
    });
});
$(document).on("mousedown", ".referee-button", function (e) {
    if (e.which === 3)
    {
        e.preventDefault();
        e.stopPropagation();

        $("#showCardOfRefereeModal").modal("show");
        $("#showCardOfRefereeModalBody").html('<div class="text-center"><span class="spinner-border"></span></div>');

        const refereeId = $(this).data("id");

        $.ajax({
            url: "Admin/Referee/GetCardInfo",
            type: "POST",
            data: { id:refereeId },
            success: function (response) {
                $("#showCardOfRefereeModalBody").html(response);
                let $suggestionsContainer = $('<div>', {
                    id: 'teamSuggestions',
                    css: {
                        position: 'absolute',
                        width: $('#vetoTeamName').outerWidth(),
                        maxHeight: '200px',
                        overflowY: 'auto',
                        border: '1px solid #ccc',
                        backgroundColor: '#fff',
                        display: 'none',
                        zIndex: 1000
                    }
                });
                $('#vetoTeamName').parent().css('position', 'relative').append($suggestionsContainer);
                loadAllCompetitions();
            },
            error: function (xhr) {
                $("#showCardOfRefereeModalBody").html("<div class='alert alert-danger'>Nepodařilo se načíst informace o rozhodčím.</div>");
            }
        });
    }
});
$(document).on("contextmenu", ".referee-button", function (e) {
    e.preventDefault();
    e.stopPropagation();
    return false;
});
$(document).on("click", ".updateField", function (e) {
    e.preventDefault();

    const id = $(this).data("id");
    const fieldName = $("#fieldName_" + id).val();
    const fieldAddress = $("#fieldAddress_" + id).val();
    const latitude = $("#latitude_" + id).val();
    const longitude = $("#longitude_" + id).val();

    var form = new FormData;
    form.append("fieldId", parseInt(id));
    form.append("fieldName", fieldName);
    form.append("fieldAddress", fieldAddress);
    form.append("latitude", parseFloat(latitude));
    form.append("longitude", parseFloat(longitude));

    $.ajax({
        url: "Admin/Field/UpdateSingleField",
        type: "POST",
        processData: false,
        contentType: false,
        data: form,
        success: function (response) {
            Swal.fire({
                icon: 'success',
                title: 'Úspěch',
                text: response
            });
        },
        error: function (xhr, status, error) {
            $("#footbalfieldsModal").modal("hide");
            if (xhr.status === 500) {
                showAlert("Error: " + xhr.responseText, "danger");
            } else {
                showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
            }
        }
    });

});
$(document).on("click", "#updateVeto", function (e) {
    e.preventDefault();

    const id = $(this).data("id");
    const noteOfVeto = $("#noteOfVeto").val();


    var form = new FormData;
    form.append("id", parseInt(id));
    form.append("note", noteOfVeto);

    $.ajax({
        url: "Admin/Veto/UpdateVeto",
        type: "POST",
        processData: false,
        contentType: false,
        data: form,
        success: function (response) {
            Swal.fire({
                icon: 'success',
                title: 'Úspěch',
                text: response
            });
        },
        error: function (xhr, status, error) {
            if (xhr.status === 500) {
                Swal.fire({
                    icon: 'error',
                    title: 'Chyba',
                    text: xhr.responseText
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Neznáma chyba',
                    text: xhr.statusText
                });
            }
        }
    });

});
$(document).on("click", "#deleteVeto", function (e) {
    e.preventDefault();

    const id = $(this).data("id");
    var form = new FormData;
    form.append("id", parseInt(id));

    $.ajax({
        url: "Admin/Veto/DeleteVeto",
        type: "POST",
        processData: false,
        contentType: false,
        data: form,
        success: function (response) {
            Swal.fire({
                icon: 'success',
                title: 'Úspěch',
                text: response
            });
        },
        error: function (xhr, status, error) {
            if (xhr.status === 500) {
                Swal.fire({
                    icon: 'error',
                    title: 'Chyba',
                    text: xhr.responseText
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Neznáma chyba',
                    text: xhr.statusText
                });
            }
        }
    });

});
$(document).on("input", "#vetoTeamName", function () {

    const searchTerm = $(this).val().trim();
    clearTimeout(searchTimeout);

    if (searchTerm.length < 3) {
        $('#teamSuggestions').hide();
        return;
    }

    searchTimeout = setTimeout(function () {
        $.ajax({
            url: `Admin/Match/GetTeamsByInput`,
            method: 'GET',
            data: { input: searchTerm },
            success: function (teams) {
                $('#teamSuggestions').empty();

                if (teams.length > 0) {
                    $.each(teams, function (index, team) {
                        $('<div>', {
                            text: team.name,
                            class: 'suggestion-item',
                            css: {
                                padding: '8px 12px',
                                cursor: 'pointer',
                                borderBottom: '1px solid #eee'
                            }
                        })
                            .hover(
                                function () { $(this).css('backgroundColor', '#f0f0f0'); },
                                function () { $(this).css('backgroundColor', '#fff'); }
                            )
                            .on('click', function () {
                                $('#vetoTeamName').val(team.name);
                                $('#vetoTeamId').val(team.teamId);
                                $('#teamSuggestions').hide();

                            })
                            .appendTo($('#teamSuggestions'));
                    });

                    $('#teamSuggestions').show();

                    $('#teamSuggestions').css({
                        top: $('#vetoTeamName').outerHeight(),
                        left: 0
                    });
                }
                else {
                    $('#teamSuggestions').hide();
                }
            },
            error: function (error) {
                if (xhr.status === 500) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Chyba',
                        text: xhr.responseText
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Neznáma chyba',
                        text: xhr.statusText
                    });
                }            }
        });
    }, 300);
});
$(document).on('click', function (event) {
    if (!$(event.target).is($('#vetoTeamName')) && !$(event.target).is($('#teamSuggestions'))) {
        $('#teamSuggestions').hide();
    }
});
$(document).on("click", "#addVeto", function () {
    const idOfReferee = $(this).data("referee-id");
    const idOfTeam = $("#vetoTeamId").val();
    const idOfCompetition = $("#vetoCompetitionId").val();
    const note = $("#newNote").val();

    if (idOfTeam == null || idOfCompetition == null ) {
        Swal.fire({
            icon: 'error',
            title: 'Chyba',
            text: "Zadejte id soutěže id týmu"
        });
        return;
    }

    var form = new FormData;
    form.append("idOfReferee", parseInt(idOfReferee));
    form.append("idOfTeam", idOfTeam);
    form.append("idOfCompetition", idOfCompetition);
    form.append("note", note);


    $.ajax({
        url: "Admin/Veto/AddVeto",
        type: "POST",
        processData: false,
        contentType: false,
        data: form,
        success: function (response) {
            Swal.fire({
                icon: 'success',
                title: 'Úspěch',
                text: response
            });
        },
        error: function (xhr, status, error) {
            if (xhr.status === 500) {
                Swal.fire({
                    icon: 'error',
                    title: 'Chyba',
                    text: xhr.responseText
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Neznáma chyba',
                    text: xhr.statusText
                });
            }
        }
    });

});
$(document).on("click", "#updateReferee", function (e) {
    e.preventDefault();

    const idNormal = $(this).data('id');
    const nameVal = $(this).data('name');
    const surnameVal = $(this).data('surname');

    const idVal = $("#refereeId").val();
    const emailVal = $("#refereeEmail").val();
    const ageVal = $("#refereeAge").val();
    const leagueVal = $("#refereeLeague").val();
    const carVal = $("#refereeCar").val();
    const pfsVal = $("#refereePfs").val();
    const placeVal = $("#refereePlace").val();
    const noteVal = $("#refereeNote").val();

    var form = new FormData;
    form.append("id", parseInt(idNormal));
    form.append("name", nameVal);
    form.append("surname", surnameVal);

    form.append("idFacr", idVal);
    form.append("email", emailVal);
    form.append("age", parseInt(ageVal));
    form.append("league", parseInt(leagueVal));
    form.append("car", carVal);
    form.append("pfs", pfsVal);
    form.append("place", placeVal);
    form.append("note", noteVal);



    // AJAX call
    $.ajax({
        url: "Admin/Referee/UpdateRefereeAsync",
        type: "POST",
        processData: false,
        contentType: false,
        data: form,
        success: function (response) {
            Swal.fire({
                icon: 'success',
                title: 'Úspěch',
                text: response
            });
        },
        error: function (xhr, status, error) {
            if (xhr.status === 500) {
                Swal.fire({
                    icon: 'error',
                    title: 'Chyba',
                    text: xhr.responseText
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Neznáma chyba',
                    text: xhr.statusText
                });
            }
        }
    });

});
$(document).on("click", "#printAllMatchesBtn", function (e) {
    e.preventDefault();
    var form = $('<form>', {
        method: 'POST',
        action: 'Admin/Match/DownloadFileWithFilledMatches',
        style: 'display: none;'
    });

    $('body').append(form);
    form.submit();
    form.remove();


});

$(document).on("click", "#addNewRefereeBtn", function (e) {
    e.preventDefault();

    $("#addNewRefereeModal").modal("show");
});
$(document).on("submit", "#newRefereeForm", function (e) {
    e.preventDefault();

    const form = $(this);

    if (!this.checkValidity()) {
        e.stopPropagation();
        form.addClass('was-validated');
        return;
    }

    const submitBtn = form.find('button[type="submit"]');
    const originalBtnText = submitBtn.html();
    submitBtn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Ukládám...');
    submitBtn.prop('disabled', true);

    const refereeData = {
        facrId: $('#facrId').val() || null,
        name: $('#name').val(),
        surname: $('#surname').val(),
        email: $('#email').val(),
        league: parseInt($('#league').val()),
        age: parseInt($('#age').val()),
        ofs: $('#ofs').prop('checked'),
        note: $('#note').val() || null,
        carAvailability: $('#carAvailability').prop('checked')
    };

    $.ajax({
        url: 'Admin/Referee/AddNewRefereeAsync',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(refereeData),
        success: function (response) {

            showAlert("Úspěch: " + response, "success");

            $("#addNewRefereeModal").modal("hide");

            form[0].reset();
            form.removeClass('was-validated');
        },
        error: function (xhr, status, error) {
            if (xhr.status === 500 || xhr.status === 400) {
                showAlert("Error: " + xhr.responseText, "danger");
            } else {
                showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
            }
        },
        complete: function () {
            submitBtn.html(originalBtnText);
            submitBtn.prop('disabled', false);
            $("#addNewRefereeModal").modal("hide");
        }
    });
});
$(document).on('hidden.bs.modal', '#addNewRefereeModal', function () {
    const form = $('#newRefereeForm');
    form[0].reset();
    form.removeClass('was-validated');
});
$(document).on("click", "#filterMatchesBasedOnDate", function () {
    const timeRange = $("#filterStartDateTime .datepicker").val();
    if (!timeRange) {
        Swal.fire({
            icon: 'error',
            title: 'Chyba',
            text: "Prosím vyberte časové rozmězí"
        });
        return;
    }
    let [dateFrom, timeFrom, dateTo, timeTo] = extractDateTime(timeRange);

    $.ajax({
        url: "Admin/Match/GetMatchesByDate",
        type: "GET",
        data: { startDate: dateFrom, startTime: timeFrom, endDate: dateTo, endTime: timeTo },
        success: function (response) {
            $('#undelegated-content').html(response);
        },
        error: function (xhr) {
            if (xhr.status === 500) {
                showAlert("Error: " + xhr.responseText, "danger");
            } else {
                showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
            }
        }

    });
});
$(document).on("click", ".match-referee-counter", function () {
    const colorMapping = {
        1: 'forestgreen',
        2: 'pink',
        3: 'orange',
        4: 'red',
        5: 'black'
    };

    const idMatch = $(this).data('id');
    const homeTeam = $(this).data('home');
    const asAReferee = $(this).data('referee');


    const matchPane = $('#match_pane_' + idMatch);

    const isDisabled = matchPane.hasClass('disabled-pane');
    if (isDisabled) {
        showAlert("Uzamčený zápas", "warning");
        return;
    }
    const getLeagueLevel = $('.tab-pane.show.active').eq(1).data('id');

    const buttonsDivName = "pane-" + getLeagueLevel;
    const $buttonsDiv = $('#' + buttonsDivName);

    const $buttons = $buttonsDiv.find('.referee-button-wrapper');

    const idsButtons = $buttons.find('.referee-button');

    const ids = $buttons
        .find('.referee-button')
        .map(function () {
            return $(this).data('id');
        })
        .get();

    let teamId;
    if (homeTeam) {
        teamId = matchPane.find('.home-team-id').text();
    } else {
        teamId = matchPane.find('.away-team-id').text();
    }
    const competitionId = matchPane.find('.competition_id').text();

    let refereeMatchCountRequest = {
        refereeIds: ids,
        teamId: teamId,
        competitionId: competitionId,
        isReferee: asAReferee
    };


    $.ajax({
        url: 'Admin/Match/GetRefereeMatchCounts',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(refereeMatchCountRequest),
        success: function (response) {
            idsButtons.each(function () {
                $(this).css('background', `linear-gradient(to right, #6c757d 50%, #6c757d 50%)`);
            });
            response.forEach(function (item) {
                const refereeId = item.refereeId;
                const homeCount = item.homeCount;
                const awayCount = item.awayCount;

                const leftColor = colorMapping[homeCount] || '#6c757d';
                const rightColor = colorMapping[awayCount] || '#6c757d';

                const buttonId = refereeId + "_referee-offer-pure-button";

                $("#" + buttonId).css('background', `linear-gradient(to right, ${leftColor} 50%, ${rightColor} 50%)`);
            });
        },
        error: function (error) {
            Swal.fire({
                icon: 'error',
                title: 'Neznáma chyba',
                text: error
            });        }
    });



});
$(document).on("click", "#color_ref_count", function () {
    const colorMapping = {
        1: 'forestgreen',
        2: 'pink',
        3: 'orange',
        4: 'orangered',
        5: 'darkred',
    };

    const getLeagueLevel = $('.tab-pane.show.active').eq(1).data('id');

    const buttonsDivName = "pane-" + getLeagueLevel;
    const $buttonsDiv = $('#' + buttonsDivName);

    const $buttons = $buttonsDiv.find('.referee-button-wrapper');

    const idsButtons = $buttons.find('.referee-button');

    const ids = $buttons
        .find('.referee-button')
        .map(function () {
            return $(this).data('id');
        })
        .get();
    const requestPayload = ids.map(id => ({ refereeId: id }));

    $.ajax({
        url: 'Admin/Match/GetRefereeMatches',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(requestPayload),
        success: function (response) {
           idsButtons.each(function () {
                $(this).css('background', `linear-gradient(to right, #6c757d 50%, #6c757d 50%)`);
            });
            response.forEach(function (item) {
                const refereeId = item.refereeId;
                const mainRef = item.asMainReferee;
                const arRef = item.asAssistantReferee;

                const leftColor = (mainRef >= 6) ? 'black' : (colorMapping[mainRef] || '#6c757d');
                const rightColor = (arRef >= 6) ? 'black' : (colorMapping[arRef] || '#6c757d');

                const buttonId = refereeId + "_referee-offer-pure-button";

                $("#" + buttonId).css('background', `linear-gradient(to right, ${leftColor} 50%, ${rightColor} 50%)`);
            });
        },
        error: function (error) {
            Swal.fire({
                icon: 'error',
                title: 'Neznáma chyba',
                text: error
            });
        }
    });



});
$(document).on("click", ".points_button_r", function () {
    const idMatch = $(this).data('id');
    const asAReferee = $(this).data('referee');

    const matchPane = $('#match_pane_' + idMatch);

    const isDisabled = matchPane.hasClass('disabled-pane');
    if (isDisabled) {
        showAlert("Uzamčený zápas", "warning");
        return;
    }
    const getLeagueLevel = $('.tab-pane.show.active').eq(1).data('id');

    const buttonsDivName = "pane-" + getLeagueLevel;
    const $buttonsDiv = $('#' + buttonsDivName);

    const $buttons = $buttonsDiv.find('.referee-button-wrapper');

    const idsButtons = $buttons.find('.referee-button');

    const ids = $buttons
        .find('.referee-button')
        .map(function () {
            return $(this).data('id');
        })
        .get();


    const homeTeam = matchPane.find('.home-team-id').text();
    const awayTeam = matchPane.find('.away-team-id').text();

    let refereeMatchPointsViewModel = {
        refereeIds: ids,
        homeTeamId: homeTeam,
        awayTeamId: awayTeam,
        matchId: idMatch,
        isReferee: asAReferee
    };


    $.ajax({
        url: 'Admin/Match/GetRefereesPoints',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(refereeMatchPointsViewModel),
        success: function (response) {
            idsButtons.each(function () {
                $(this).css('background', `linear-gradient(to right, #6c757d 50%, #6c757d 50%)`);
            });
            ids.forEach(function (refereeId) {
                if (response.hasOwnProperty(refereeId)) {
                    const points = response[refereeId].Item1 || response[refereeId].item1;
                    const messageForTippy = response[refereeId].Item2 || response[refereeId].item2;


                    const color = getColorForValue(points);

                    const buttonId = refereeId + "_referee-offer-pure-button";
                    $("#" + buttonId).css('background', color);

                    if (messageForTippy) {
                        const htmlMessage = messageForTippy.replace(/\n/g, '<br>');
                        $("#" + buttonId)
                            .attr('data-bs-original-title', htmlMessage)
                            .attr('data-bs-html', 'true')
                            .tooltip('dispose')
                            .tooltip({ html: true });
                    }
                }
            });
        },
        error: function (error) {
            Swal.fire({
                icon: 'error',
                title: 'Neznáma chyba',
                text: error
            });
        }
    });



});
$(document).on("click", ".points_button_ar", function () {
    const idMatch = $(this).data('id');
    const asAReferee = $(this).data('referee');

    const matchPane = $('#match_pane_' + idMatch);

    const isDisabled = matchPane.hasClass('disabled-pane');
    if (isDisabled) {
        showAlert("Uzamčený zápas", "warning");
        return;
    }
    const getLeagueLevel = $('.tab-pane.show.active').eq(1).data('id');

    const buttonsDivName = "pane-" + getLeagueLevel;
    const $buttonsDiv = $('#' + buttonsDivName);

    const $buttons = $buttonsDiv.find('.referee-button-wrapper');

    const idsButtons = $buttons.find('.referee-button');

    const ids = $buttons
        .find('.referee-button')
        .map(function () {
            return $(this).data('id');
        })
        .get();


    const homeTeam = matchPane.find('.home-team-id').text();
    const awayTeam = matchPane.find('.away-team-id').text();


    let refereeMatchPointsViewModel = {
        refereeIds: ids,
        homeTeamId: homeTeam,
        awayTeamId: awayTeam,
        matchId: idMatch,
        isReferee: asAReferee
    };


    $.ajax({
        url: 'Admin/Match/GetRefereesPoints',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(refereeMatchPointsViewModel),
        success: function (response) {
            idsButtons.each(function () {
                $(this).css('background', `linear-gradient(to right, #6c757d 50%, #6c757d 50%)`);
            });
            ids.forEach(function (refereeId) {
                if (response.hasOwnProperty(refereeId)) {
                    const points = response[refereeId].Item1 || response[refereeId].item1;
                    const messageForTippy = response[refereeId].Item2 || response[refereeId].item2;


                    const color = getColorForValue(points);

                    const buttonId = refereeId + "_referee-offer-pure-button";
                    $("#" + buttonId).css('background', color);

                    if (messageForTippy) {
                        const htmlMessage = messageForTippy.replace(/\n/g, '<br>');
                        $("#" + buttonId)
                            .attr('data-bs-original-title', htmlMessage)
                            .attr('data-bs-html', 'true')
                            .tooltip('dispose')
                            .tooltip({ html: true });
                    }
                }
            });
        },
        error: function (error) {
            Swal.fire({
                icon: 'error',
                title: 'Neznáma chyba',
                text: error
            });
        }
    });



});

$(document).on("click", ".referee-place", function () {
    var $refereePlace = $(this);
    var $parentDiv = $refereePlace.closest('.position-relative');

    var $matchPane = $refereePlace.closest('.match_pane');

    var isDisabled = $matchPane.hasClass('disabled-pane');
    if (isDisabled) {
        showAlert("Uzamčený zápas", "warning");
        return;
    }

    if ($parentDiv.children().length > 1) {
        var buttonDataId = $parentDiv.find('.referee-button').data('id');
        var idMatch = $(this).data('id');
        var userVal = $("#usernameLogged").text();

        var matchDetails = {
            matchId: idMatch,
            refereeId: buttonDataId,
	    user:userVal
        };

        $.ajax({
            url: 'Admin/Referee/RemoveRefereeFromTheMatch',
            type: 'POST',
            data: matchDetails,
            success: function (response) {
                $parentDiv.find('button.referee-button').remove();

            },
            error: function (xhr, status, error) {
                if (xhr.status === 500) {
                    showAlert("Error: " + xhr.responseText,"danger");
                } else {
                    showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
                }
            }
        });
    }
    else {
        if (selectedRefereeId) {
            var idMatch = $(this).data('id');
            var roleType = Number($(this).data('role'));
            var userVal = $("#usernameLogged").text();

            var matchDetails = {
                matchId: idMatch,
                refereeId: selectedRefereeId,
                role: roleType,
                force:false,
		user:userVal
            };

            $.ajax({
                url: 'Admin/Referee/AddRefereeToTheMatch',
                type: 'POST',
                data: matchDetails,
                success: function (response) {

                },
                error: function (xhr, status, error) {
                    if (xhr.status === 400) {
                        showAlert(xhr.responseText, "warning", function () {
                            const matchDetailsWithForce = {
                                matchId: idMatch,
                                refereeId: selectedRefereeId,
                                role: roleType,
                                force: true,
				user:userVal
                            };

                            $.ajax({
                                url: 'Admin/Referee/AddRefereeToTheMatch',
                                type: 'POST',
                                data: matchDetailsWithForce,
                                success: function (response) {
                                    Swal.fire({
                                        icon: 'success',
                                        title: 'Úspěch',
                                        text: "Rozhodčí byl přidělen i přes nedostupnost."
                                    });
                                },
                                error: function (xhr2, status2, error2) {
                                    Swal.fire({
                                        icon: 'error',
                                        title: 'Chyba',
                                        text: "Rozhodčí byl přidělen i přes nedostupnost." + xhr2.responseText
                                    });
                                }
                            });
                        });
                    }
                    else if (xhr.status === 500) {
                        showAlert("Error: " + xhr.responseText,"danger");
                    } else {
                        showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
                    }
                }
            });
        } else {
            showAlert('Vyberte rozhodčího předtím, než kliknete na zápas.',"info");
        }
    }

});
$(document).on("click", ".lock-button", function () {
        const idMatch = $(this).data('id');
        var userVal = $("#usernameLogged").text();

        const matchDetails = {
            matchId: idMatch,
	    user: userVal

        };

        $.ajax({
            url: 'Admin/Match/LockOrUnlockMatch',
            type: 'POST',
            data: matchDetails,
            error: function (xhr, status, error) {
                if (xhr.status === 500) {
                    showAlert("Error: " + xhr.responseText, "danger");
                } else {
                    showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
                }
            }
        });
});
$(document).on("click", ".enclose-match-button", function () {
    const idMatch = $(this).data('id');
    var userVal = $("#usernameLogged").text();

    const matchDetails = {
        matchId: idMatch,
	user: userVal

    };

    $.ajax({
        url: 'Admin/Match/MakeMatchPlayed',
        type: 'POST',
        data: matchDetails,
        success: function (response) {
            showAlert("Úspěch: " + response, "success");
        },
        error: function (xhr, status, error) {
            if (xhr.status === 500) {
                showAlert("Error: " + xhr.responseText, "danger");
            } else {
                showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
            }
        }
    });
});
$(document).on("click", ".save-match-button", function () {
    const idMatch = $(this).data('id');
    const preMatchValue = $('#match_input_prematch_' + idMatch).val();
    const postMatchValue = $('#match_input_postmatch_' + idMatch).val();
    const matchDetails = {
        matchId: idMatch,
        preMatch: preMatchValue,
        postMatch:postMatchValue
    };

    $.ajax({
        url: 'Admin/Match/UpdateMatchesPreAPostMatch',
        type: 'POST',
        data: matchDetails,
        success: function (response) {
            showAlert("Úspěch: " + response, "success");
        },
        error: function (xhr, status, error) {
            if (xhr.status === 500) {
                showAlert("Error: " + xhr.responseText, "danger");
                setTimeout(function () {
                    location.reload();
                }, 3000);
            } else {
                showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
                setTimeout(function () {
                    location.reload();
                }, 3000);
            }
        }
    });
});


$(document).on("click", "#importAllReferees", function (event) {
    $("#fileInputReferees").trigger("click");

});
$(document).on("change", "#fileInputReferees", function (event) {

    var file = event.target.files[0];
    var allowedExtensions = ["application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"];

    if (file && allowedExtensions.includes(file.type)) {
        var formData = new FormData();
        formData.append("file", file);

        $.ajax({
            url: "Admin/Referee/UploadRefereesFromFileAsync",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
                showAlert("Úspěch: " + response,"success");
            },
            error: function (xhr) {
                if (xhr.status === 500) {
                    showAlert("Error: " + xhr.responseText, "danger");
                } else {
                    showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
                }
            }

        });
    } else {
        showAlert("Prosím vyberte validní excel soubor!","danger");
    }
});
$(document).on("click", "#importAllEmailsPhoneNumbers", function (event) {
    $("#fileInputEmails").trigger("click");

});
$(document).on("change", "#fileInputEmails", function (event) {

    var file = event.target.files[0];
    var allowedExtensions = ["application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"];

    if (file && allowedExtensions.includes(file.type)) {
        var formData = new FormData();
        formData.append("file", file);

        $.ajax({
            url: "Admin/Referee/UploadRefereesFromEmailFileAsync",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
                showAlert("Úspěch: " + response, "success");
            },
            error: function (xhr) {
                if (xhr.status === 500) {
                    showAlert("Error: " + xhr.responseText, "danger");
                } else {
                    showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
                }
            }

        });
    } else {
        showAlert("Prosím vyberte validní excel soubor!", "danger");
    }
});
$(document).on("click", "#importAllMatches", function (event) {
    $("#fileInputMatches").trigger("click");

});
$(document).on("change", "#fileInputMatches", function (event) {

    var file = event.target.files[0];
    var allowedExtensions = ["application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"];
    var user = $("#usernameLogged").text();

    if (file && allowedExtensions.includes(file.type)) {
        var formData = new FormData();
        formData.append("file", file);
	formData.append("user", user);	

        $.ajax({
            url: "Admin/Match/UploadMatchesFromFileAsync",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
                Swal.fire({
                    icon: 'success',
                    title: 'Úspěch',
                    text: "Úspěch: " + response
                });
            },
            error: function (xhr) {
                if (xhr.status === 500) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Chyba',
                        text: "Error: " + xhr.responseText
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Neznáma chyba',
                        text: "An unexpected error occurred: " + xhr.statusText
                    });
                }
            }

        });
    } else {
        showAlert("Prosím vyberte validní excel soubor!", "danger");
    }
});
$(document).on("click", "#importDataAboutFields", function (event) {
    $("#fileInputFields").trigger("click");

});
$(document).on("change", "#fileInputFields", function (event) {

    var file = event.target.files[0];
    var allowedExtensions = ["application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"];

    if (file && allowedExtensions.includes(file.type)) {
        var formData = new FormData();
        formData.append("file", file);

        $.ajax({
            url: "Admin/Field/UploadFieldsInformationsFromFileAsync",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
                showAlert("Úspěch: " + response, "success");
            },
            error: function (xhr) {
                if (xhr.status === 500) {
                    showAlert("Error: " + xhr.responseText, "danger");
                } else {
                    showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
                }
            }

        });
    } else {
        showAlert("Prosím vyberte validní excel soubor!", "danger");
    }
});
$(document).on("change", "#sort_first_row", function (event) {

        const changedTo = $('#sort_first_row').val();

        $.ajax({
            url: "Admin/Match/GetSortedMatchesAsync",
            type: "GET",
            data: { selector: changedTo },
            success: function (response) {
                $('#undelegated-content').html(response);
            },
            error: function (xhr) {
                if (xhr.status === 500) {
                    showAlert("Error: " + xhr.responseText, "danger");
                } else {
                    showAlert("An unexpected error occurred: " + xhr.statusText,"danger");
                }
            }

        });

});
$(document).on("click", "#connectMatches", function (event) {
    $.ajax({
        url: "Admin/Match/MakeConnectionsOfMatches",
        type: "POST",
        success: function (response) {
            $('#undelegated-content').html(response);
        },
        error: function (xhr) {
            if (xhr.status === 500) {
                showAlert("Error: " + xhr.responseText, "danger");
            } else {
                showAlert("An unexpected error occurred: " + xhr.statusText, "danger");
            }
        }

    });
});
$(document).on("click", "#loadPreviousMatches", function (event) {
    $("#fileInputPlayedMatches").trigger("click");

});
$(document).on("change", "#fileInputPlayedMatches", function (event) {

    var file = event.target.files[0];
    var allowedExtensions = ["application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"];
    var user = $("#usernameLogged").text();
    if (file && allowedExtensions.includes(file.type)) {
        var formData = new FormData();
        formData.append("file", file);
	formData.append("user", user);	
	
	$.ajax({
            url: "Admin/Match/UploadMatchesFromFileAsync",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
		 $.ajax({
            		url: "Admin/Match/UploadPlayedMatchesFromFileAsync",
            		type: "POST",
            		data: formData,
            		contentType: false,
            		processData: false,
            		success: function (response) {
                		showAlert("Úspěch: " + response,"success");
            		},
            		error: function (xhr) {
                	if (xhr.status === 500) {
                    		showAlert("Error: " + xhr.responseText, "danger");
                	} else {
                    		showAlert("An unexpected error occurred: " + xhr.statusText,"danger");
                	}
            	}

        	});
            },
            error: function (xhr) {
                if (xhr.status === 500) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Chyba',
                        text: "Error: " + xhr.responseText
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Neznáma chyba',
                        text: "An unexpected error occurred: " + xhr.statusText
                    });
                }
            }

        });

    } else {
        showAlert("Prosím vyberte validní excel soubor!", "danger");
    }
});

//HELPER METHODS
function showAlert(message, type = "warning", confirmCallback = null) {
    $("#customAlertText").text(message);

    $("#customAlert")
        .removeClass("d-none alert-warning alert-success alert-danger alert-info")
        .addClass(`alert-${type} show`);

    $("#customConfirmButton").remove();

    if (confirmCallback) {
        const confirmButton = $('<button id="customConfirmButton" class="btn btn-sm btn-danger ms-2">Přidělit rozhodčího i přes to</button>');
        $("#customAlertText").append(confirmButton);

        $(document).off('click', '#customConfirmButton').on('click', '#customConfirmButton', function () {
            confirmCallback();
            $("#customAlert").addClass("d-none");
        });
    }
}
function extractDateTime(timeRange) {
    if (!timeRange) return [null, null, null, null];

    const parts = timeRange.split(" to ");
    if (parts.length !== 2) return [null, null, null, null];

    const [dateFrom, timeFrom] = parts[0].split(" ");
    const [dateTo, timeTo] = parts[1].split(" ");

    return [dateFrom, timeFrom + ":00", dateTo, timeTo + ":00"];
}
function loadAllCompetitions() {
    $('#vetoCompetitionName').html('<option selected disabled>Vyberte název soutěže</option>');
    $('#vetoCompetitionId').html('<option selected disabled>Vyberte id soutěže</option>');
    $('#vetoCompetitionName').append(
        $('<option>', {
            value: "all",
            text: "all",
            'data-id': "all"
        })
    );
    $('#vetoCompetitionId').append(
        $('<option>', {
            value: "all",
            text: "all",
            'data-name': "all"
        })
    );

    $.ajax({
        url: 'Admin/Match/GetCompetitions',
        method: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response && response.length > 0) {
                $.each(response, function (index, competition) {
                        $('#vetoCompetitionName').append(
                            $('<option>', {
                                value: competition.competitionName,
                                text: competition.competitionName,
                                'data-id': competition.competitionId
                            })
                        );

                        $('#vetoCompetitionId').append(
                            $('<option>', {
                                value: competition.competitionId,
                                text: competition.competitionId,
                                'data-name': competition.competitionName
                            })
                        );
                });

                // Set up synchronization between dropdowns
                $('#vetoCompetitionName').on('change', function () {
                    let dataId = $('#vetoCompetitionName option:selected').data('id');
                    $('#vetoCompetitionId').val(dataId);
			
                });

                $('#vetoCompetitionId').on('change', function () {
                    let dataName = $('#vetoCompetitionId option:selected').data('name');
                    $('#vetoCompetitionName').val(dataName);
                });

            } else {
                Swal.fire({
                    icon: "error",
                    title: "Chyba",
                    text: "Nepodařilo se načíst soutěže. Prosím zkuste to znovu později."
                });
            }
        },
        error: function (xhr, status, error) {
            Swal.fire({
                icon: "error",
                title: "Chyba",
                text: "Nepodařilo se načíst soutěže. Prosím zkuste to znovu později."
            });
        }
    });
}
// Define color stops for the gradient
const colorStops = [
    { value: 0, color: '#8B0000' },  // darkred
    { value: 25, color: '#FF0000' }, // red
    { value: 50, color: '#FFA500' }, // orange
    { value: 75, color: '#9ACD32' }, // yellowgreen
    { value: 100, color: '#008000' } // green
];

function getColorForValue(value) {
    value = Math.max(0, Math.min(100, value));

    for (let stop of colorStops) {
        if (stop.value === value) return stop.color;
    }

    let lowerStop = colorStops[0];
    let upperStop = colorStops[colorStops.length - 1];

    for (let i = 0; i < colorStops.length - 1; i++) {
        if (value >= colorStops[i].value && value <= colorStops[i + 1].value) {
            lowerStop = colorStops[i];
            upperStop = colorStops[i + 1];
            break;
        }
    }

    const range = upperStop.value - lowerStop.value;
    const pos = (value - lowerStop.value) / range;

    function interpolateComponent(c1, c2, position) {
        return Math.round(c1 + position * (c2 - c1));
    }

    function hexToRgb(hex) {
        const r = parseInt(hex.slice(1, 3), 16);
        const g = parseInt(hex.slice(3, 5), 16);
        const b = parseInt(hex.slice(5, 7), 16);
        return { r, g, b };
    }

    function rgbToHex(r, g, b) {
        return '#' +
            ((1 << 24) + (r << 16) + (g << 8) + b)
                .toString(16)
                .slice(1);
    }

    const color1 = hexToRgb(lowerStop.color);
    const color2 = hexToRgb(upperStop.color);

    const r = interpolateComponent(color1.r, color2.r, pos);
    const g = interpolateComponent(color1.g, color2.g, pos);
    const b = interpolateComponent(color1.b, color2.b, pos);

    return rgbToHex(r, g, b);
}
function deleteAllCookies() {
        $.each(document.cookie.split(";"), function (i, cookie) {
            var cookieName = $.trim(cookie.split("=")[0]);

            // Delete cookie for current path
            document.cookie = cookieName + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";

            // Optional: delete for parent domain too
            var domainParts = window.location.hostname.split(".");
            if (domainParts.length > 1) {
                var rootDomain = "." + domainParts.slice(-2).join(".");
                document.cookie = cookieName + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=" + rootDomain + ";";
            }
        });
    }

function logout() {
        deleteAllCookies();
        window.location.href = "https://rozhodcipraha.cz"; 
	}
