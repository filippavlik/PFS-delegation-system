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
    loadExcusesFromStorage();
    loadVehicleSlotsFromStorage();

});
// Array to store excuses
let excuses = [];
//Array to store vehicle availability
let vehicleSlots = [];

// Save excuses to localStorage when the list is updated
function saveExcusesToStorage() {
    localStorage.setItem("excuses", JSON.stringify(excuses));
}
function saveVehicleSlotToStorage() {
    localStorage.setItem("vehicleSlots", JSON.stringify(vehicleSlots));
}

$("#logOutTheUser").on("click", function (event) {
	$.ajax({
    		url: "/Referee/Logout",
    		type: "POST",
    		success: function () {
        		document.cookie = "auth_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;"; // Clear cookie
        		window.location.href = "https://rozhodcipraha.cz/Account/Logout"; // Redirect to login domain
    			},
    		error: function () {
        		alert("Neúspešný pokus o odhlášení!");
    		}
	});
});

// Load excuses from localStorage when the page loads
function loadExcusesFromStorage() {
    const storedExcuses = localStorage.getItem("excuses");
    if (storedExcuses) {
        excuses = JSON.parse(storedExcuses);
        renderExcusesList();
    }
}
function loadVehicleSlotsFromStorage() {
    const storedSlots = localStorage.getItem("vehicleSlots");
    if (storedSlots) {
        vehicleSlots = JSON.parse(storedSlots);
        renderVehicleSlotsList();
    }
}

// Show/hide no excuses message
function updateNoExcusesMessage() {
    if (excuses.length === 0) {
        $("#noExcusesMessage").show();
    } else {
        $("#noExcusesMessage").hide();
    }
}
// Show/hide no vehicleSlots message
function updateNoSlotMessage() {
    if (vehicleSlots.length === 0) {
        $("#noVehicleSlotsMessage").show();
    } else {
        $("#noVehicleSlotsMessage").hide();
    }
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
// Render the excuses list
function renderVehicleSlotsList() {
    $("#vehicleSlotList").empty();  // Clear the current list

    // Loop through the excuses array and create list items
    $.each(vehicleSlots, function (index, vehicleSlot) {
        const $li = $("<li>");

        const $infoDiv = $("<div>").addClass("slot-info");

        // Add time, choice
 
        const $timeSpan = $("<div>").addClass("slot-time").text(vehicleSlot.timeRange);
        const $vehicleChoice = $("<div>").addClass("slot-choice").text(vehicleSlot.vehicleChoice);

        // Add delete button functionality
        const $deleteBtn = $("<span>")
            .addClass("delete-slot")
            .html('<i class="fa fa-trash"></i>')
            .on("click", function () {
                // Remove excuse from the array
                vehicleSlots.splice(index, 1);
                renderVehicleSlotsList();  // Re-render list
                updateNoSlotMessage();  // Update message
            });

        // Append the elements
        $infoDiv.append($timeSpan, $vehicleChoice);
        $li.append($infoDiv, $deleteBtn);
        $("#vehicleSlotList").append($li);  // Add the list item to the list
    });

    updateNoSlotMessage();  // Update the "no excuses" message
}

// Add vehicle slot to the list
$("#submitNewVehicleSlotToList").on("click", function (event) {
    event.preventDefault(); // Prevents form submission
    const timeRange = $("#inputDateTimeVehicle .datepicker").val();  // Get start time
    const vehicleChoice = $("input[name='carStatus']:checked").val();

    // Check if both fields are filled
    if (!timeRange) {
        alert("Prosím vyberte časové rozmězí");
        return;
    }
    if (!vehicleChoice) {
        alert("Prosím vyberte možnosti vozidla");
        return;
    }


    // Add the new excuse to the array
    vehicleSlots.push({
        timeRange: timeRange,
        vehicleChoice: vehicleChoice
    });

    // Clear input fields after adding the excuse
    $("#inputDateTimeVehicle")[0]._flatpickr.clear();
    $("input[name='carStatus']").prop("checked", false); // Uncheck all radio buttons

    // Update the list display
    renderVehicleSlotsList();
    saveVehicleSlotToStorage();  // Save to localStorage
});

// Add excuse to the list
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
        saveExcusesToStorage();  // Save to localStorage

    });

// Submit all excuses
$("#sendNewExcuse").on("click", function (event) {
    event.preventDefault(); // Prevents form submission
    if (excuses.length === 0) {
        alert("Nejsou přidány žádné omluvy");
        return;
    }

    //send the excuses to a server via AJAX 
    
    $.ajax({
        url: "/Referee/SaveExcuse",  // Matches HomeController route
        type: "POST",
        data: JSON.stringify({
		userId: $('#userId').val(),
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
        }
    });

    // Clear the list
    excuses = [];
    localStorage.removeItem("excuses");


    $('.overlay-backdrop').css('visibility', 'hidden');
    $('#excuseCard').css('visibility', 'hidden');
});

// Submit all vehicle slots
$("#sendNewVehicleSlot").on("click", function (event) {
    event.preventDefault(); // Prevents form submission
    if (vehicleSlots.length === 0) {
        alert("Nejsou přidány žádné možnosti vozidla");
        return;
    }
    
    let requestData = JSON.stringify({
    	userId: $('#userId').val(),
    	vehicleSlots: vehicleSlots.map(vehicleSlot => {
        	let [dateFrom, timeFrom, dateTo, timeTo] = extractDateTime(vehicleSlot.timeRange);
        	return {
            	dateFrom: dateFrom,
            	timeFrom: timeFrom,
            	dateTo: dateTo,
            	timeTo: timeTo,
            	hasCarInTheSlot: vehicleSlot.vehicleChoice === "available"
        	};
    	})
	});

    //send the slots to a server via AJAX
    $.ajax({
        url: "/Referee/SaveVehicleAvailability",
        type: "POST",
        data: requestData,
        contentType: "application/json",
        success: function (response) {
            alert(response.message); // Display success message from backend
            vehicleSlots = [];  // Clear the array after successful submission
            renderVehicleSlotsList();
            updateNoSlotMessage();
        },
        error: function (error) {
            alert("Došlo k chybě při ukládání: " + (error.responseJSON?.message || "Neznámá chyba"));
        }
    });

    // Clear the list
    vehicleSlots = [];
    localStorage.removeItem("vehicleSlots");


    $('.overlay-backdrop').css('visibility', 'hidden');
    $('#vehicleAvailabilityCard').css('visibility', 'hidden');
});

// Show excuse windows
$("#addNewExcuse").on("click", function () {
    $('.overlay-backdrop').css('visibility', 'visible');
    $('#excuseCard').css('visibility', 'visible');
}); 
// Cancel excuse windows
$("#cancelSendingExcuse").on("click", function () {
    // Clear the list
    excuses = [];
    localStorage.removeItem("excuses");


    $('.overlay-backdrop').css('visibility', 'hidden');
    $('#excuseCard').css('visibility', 'hidden');
}); 

// Show vehicle window
$("#addNewVehicleOptions").on("click", function () {
    $('.overlay-backdrop').css('visibility', 'visible');
    $('#vehicleAvailabilityCard').css('visibility', 'visible');
});
// Cancel vehicle windows
$("#cancelSendingVehicleSlot").on("click", function () {
    // Clear the list
    vehicleSlots = [];
    localStorage.removeItem("vehicleSlots");


    $('.overlay-backdrop').css('visibility', 'hidden');
    $('#vehicleAvailabilityCard').css('visibility', 'hidden');
}); 

//Show user informations
$("#showUserInfo").on("click", function () {
    if ($('#userInfo').css('visibility') === 'visible') {
        $('#userInfo').css('visibility', 'hidden');
    } else {
        $('#userInfo').css('visibility', 'visible');
    }

}); 

//Show preview of given vehicle availability
$("#showVehicleAvailabilityOfUser").on("click", function () {
    $('#vehicleSlotsPreview').css('visibility', 'visible');
    $('.overlay-backdrop').css('visibility', 'visible');

});
//Hide preview of given vehicle availability
$("#cancelVehicleSlotsPreview").on("click", function () {
    $('#vehicleSlotsPreview').css('visibility', 'hidden');
    $('.overlay-backdrop').css('visibility', 'hidden');

});

//Show preview of given excuses
$("#showExcusesOfUser").on("click", function (event) {
    event.preventDefault(); // Prevents form submission

    //send the slots to a server via AJAX
    $.ajax({
        url: "/Referee/GetExcuses",
        type: "GET",
        data: { userId: $('#userId').val() },
        contentType: "application/json",
        success: function (result) {
            $('#tableOfExcuses').html(result);
	},
        error: function (error) {
            alert("Došlo k chybě při získavaní omluv: " + (error.responseJSON?.message || "Neznámá chyba"));
        }
    });

    $('#excusesPreview').css('visibility', 'visible');
    $('.overlay-backdrop').css('visibility', 'visible');

});

//Show preview of given vehicle slot
$("#showVehicleAvailabilityOfUser").on("click", function (event) {
    event.preventDefault(); // Prevents form submission

    //send the slots to a server via AJAX
    $.ajax({
        url: "/Referee/GetVehicleSlots",
        type: "GET",
        data: { userId: $('#userId').val() },
        contentType: "application/json",
        success: function (result) {
		    $('#tableOfVehicleSlots').html(result);
        },
        error: function (error) {
            alert("Došlo k chybě při získavaní možností vozidla: " + (error.responseJSON?.message || "Neznámá chyba"));
        }
    });

    $('#vehicleSlotsPreview').css('visibility', 'visible');
    $('.overlay-backdrop').css('visibility', 'visible');

});
$(document).on('click', '.delete-excuse', function(event) {
	event.preventDefault(); // Prevents form submission
	var excuseIdToRemove = $(this).data('id');
	//remove the excuse from context
	$.ajax({
        	url: "/Referee/RemoveExcuse",
        	type: "POST",
        	data: { excuseId: excuseIdToRemove },
        	success: function (result) {
		    $('#showExcusesOfUser').click();
        	}	,
        	error: function (error) {
            		alert("Došlo k chybě při mazání omluvy: " + (error.responseJSON?.message || "Neznámá chyba"));
        	}
   	});
});

$(document).on('click', '.delete-vehicle-slot', function(event) {
        event.preventDefault(); // Prevents form submission
        var vehicleSlotIdToRemove = $(this).data('id');
        //remove the slot from the context
        $.ajax({
                url: "/Referee/RemoveVehicleSlot",
                type: "POST",
                data: { vehicleSlotId: vehicleSlotIdToRemove },
                success: function (result) {
                    $('#showVehicleAvailabilityOfUser').click();
                }       ,
                error: function (error) {
                        alert("Došlo k chybě při mazání možnosti vozidla: " + (error.responseJSON?.message || "Neznámá chyba"));                  }
        });
});

//Hide preview of given excuses
$("#cancelExcusesPreview").on("click", function () {
    $('#excusesPreview').css('visibility', 'hidden');
    $('.overlay-backdrop').css('visibility', 'hidden');
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
