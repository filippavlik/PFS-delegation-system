var excuses = [];

$(document).ready(function () {
    // declaring the format of input date & time field for excuses
    flatpickr("#inputDateTime", {
        enableTime: true,        // Enable time selection
        mode: "range",           // Enable range selection (start and end)
        wrap: true,              // Wrap input for custom behavior
        altInput: true,          // Use a custom input field instead of a standard one
        dateFormat: "Y-m-d H:i", // Date and time format
        minDate: "today",        // Disable past dates
        time_24hr: true,         // Use 24-hour time format
        minuteIncrement: 1,      // Increment minutes by 1
        plugins: [new rangePlugin({ input: "#inputEndDateTime" })], // Adding the rangePlugin
    });
    // declaring the format of input date & time field for vehicle
    flatpickr("#inputDateTimeVehicle", {
        enableTime: true,        // Enable time selection
        mode: "range",           // Enable range selection (start and end)
        wrap: true,              // Wrap input for custom behavior
        altInput: true,          // Use a custom input field instead of a standard one
        dateFormat: "Y-m-d H:i", // Date and time format
        minDate: "today",        // Disable past dates
        time_24hr: true,         // Use 24-hour time format
        minuteIncrement: 1,      // Increment minutes by 1
        plugins: [new rangePlugin({ input: "#inputEndDateTimeVehicle" })], // Adding the rangePlugin
    });

});
$("#submitNewExcuseToList").on("click", function (event) {
    event.preventDefault(); // Prevents form submission
    const timeRange = $("#inputDateTime .datepicker").val();  // Get start time
    const reason = $("#excuseReason").val().trim();  // Get reason
    const note = $("#excuseNote").val().trim();  // Get note

    // Check if both fields are filled
    if (!timeRange) {
        alert("Prosím vyberte časové rozmězí");
        return;
    }

    // Add the new excuse to the array
    excuses.push({
        timeRange: timeRange,
        reason: reason,
        note: note
    });

    // Clear input fields after adding the excuse
    $("#inputDateTime")[0]._flatpickr.clear();
    $("#excuseReason").val("");
    $("#excuseNote").val("");

    // Update the list display
    renderExcusesList();

});

$("#sendNewExcuse").on("click", function (event) {
    event.preventDefault(); // Prevents form submission
    if (excuses.length === 0) {
        alert("Nejsou přidány žádné omluvy");
        return;
    }

    //send the excuses to a server via AJAX
    $.ajax({
        url: "Admin/Referee/SaveExcuse",  // Matches HomeController route
        type: "POST",
        data: JSON.stringify({
            refereeId: $('#addNewExcuse').data('id'),
            excuses: excuses.map(excuse => {
                let [dateFrom, timeFrom, dateTo, timeTo] = extractDateTime(excuse.timeRange);
                return {
                    dateFrom: dateFrom,
                    timeFrom: timeFrom,
                    dateTo: dateTo,
                    timeTo: timeTo,
                    reason: excuse.reason,
                    note: excuse.note
                };
            })

        }),
        contentType: "application/json",
        success: function (response) {
            alert(response.message); // Display success message from backend
            excuses = [];  // Clear the array after successful submission
            renderExcusesList();
            updateNoExcusesMessage();
        },
        error: function (error) {
            alert("Došlo k chybě při ukládání: " + (error.responseJSON?.message || "Neznámá chyba"));
            $('#excuseCard').css('display', 'none');
        }
    });

    // Clear the list
    excuses = [];

    $('.overlay-backdrop').css('visibility', 'hidden');
    $('#excuseCard').css('display', 'none');
});

// Show excuse windows
$("#addNewExcuse").on("click", function (event) {
    $("#excusesList").empty();  // Clear the current list
    excuses = [];  // Clear the excuses array
    $('.overlay-backdrop').css('visibility', 'visible');
    $('#excuseCard').css('display', 'flex');
});

// Cancel excuse windows
$("#cancelSendingExcuse").on("click", function (event) {
    event.preventDefault();
    // Clear the list
    excuses = [];

    $('.overlay-backdrop').css('visibility', 'hidden');
    $('#excuseCard').css('display', 'none');
});

//HELPER METHODS
function extractDateTime(timeRange) {
    if (!timeRange) return [null, null, null, null];

    const parts = timeRange.split(" to ");
    if (parts.length !== 2) return [null, null, null, null];

    const [dateFrom, timeFrom] = parts[0].split(" ");
    const [dateTo, timeTo] = parts[1].split(" ");

    return [dateFrom, timeFrom + ":00", dateTo, timeTo + ":00"]; // Append ":00" for seconds
}

// Render the excuses list
function renderExcusesList() {
    $("#excusesList").empty();  // Clear the current list

    // Loop through the excuses array and create list items
    $.each(excuses, function (index, excuse) {
        const $li = $("<li>");

        const $infoDiv = $("<div>").addClass("excuse-info");

        // Add time, note, and reason to the excuse info
        const $timeSpan = $("<div>").addClass("excuse-time").text(excuse.timeRange);
        const $noteSpan = $("<div>").addClass("excuse-note").text(excuse.note);
        const $reasonSpan = $("<div>").addClass("excuse-reason").text(excuse.reason);

        // Add delete button functionality
        const $deleteBtn = $("<span>")
            .addClass("delete-excuse")
            .html('<i class="fa fa-trash"></i>')
            .on("click", function () {
                // Remove excuse from the array
                excuses.splice(index, 1);
                renderExcusesList();  // Re-render list
                updateNoExcusesMessage();  // Update message
            });

        // Append the elements
        $infoDiv.append($timeSpan, $noteSpan, $reasonSpan);
        $li.append($infoDiv, $deleteBtn);
        $("#excusesList").append($li);  // Add the list item to the list
    });

    updateNoExcusesMessage();  // Update the "no excuses" message
}

// Show/hide no excuses message
function updateNoExcusesMessage() {
    if (excuses.length === 0) {
        $("#noExcusesMessage").show();
    } else {
        $("#noExcusesMessage").hide();
    }
}
