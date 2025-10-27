var currentlyFocusedElement;
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/Admin/hubForReendering", {
        // Enable WebSockets transport explicitly
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets,
        // Add these options to help with potential proxy issues
        headers: { "X-Requested-With": "XMLHttpRequest" }
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000]) // Retry with backoff
    .configureLogging(signalR.LogLevel.Debug) // Use Debug for more detailed logs
    .build();

function startConnection() {
    console.log("Attempting to connect to SignalR hub...");
    
    connection.start()
        .then(() => {
            console.log("SignalR connection established successfully!");
        })
        .catch(err => {
            console.error("SignalR Connection Error:", err);
            
            // Log more details about the environment
            console.log("Current URL:", window.location.href);
            console.log("Hub URL:", "/Admin/hubForReendering");
            
            // Try to reconnect after delay
            console.log("Retrying connection in 5 seconds...");
            setTimeout(startConnection, 5000);
        });
}

connection.onclose(error => {
    console.log("SignalR connection closed.", error ? "Error: " + error : "");
    console.log("Attempting to reconnect...");
    
    setTimeout(startConnection, 5000);
});
//TOOLTIP AND SIGNALISING FOCUS ON MATCH CARD
$(document).on("mouseover", ".match_pane", function () {
    const user = $("#usernameLogged").text();
    const elementId = $(this).data('id');
    if(currentlyFocusedElement != elementId){
	const connectionId = connection.connectionId;

    if (elementId) {
	    console.log("ENTERED "+elementId+" BY:"+user);
	    currentlyFocusedElement = elementId;
        $.post(`Admin/Match/SendFocus?elementId=${elementId}&username=${encodeURIComponent(user)}&connectionId=${connectionId}`);
    	}
    }
});

$(document).on("mouseleave", ".match_pane", function () {
         const elementId = $(this).data('id');
	        const connectionId = connection.connectionId;

    if (elementId) {
	console.log("LEFT "+elementId);
         currentlyFocusedElement = "NONE";
	    $.post(`Admin/Match/SendRelease?elementId=${elementId}&connectionId=${connectionId}`);
    }
});
//FOCUS SIGNALIZATION
connection.on("ElementFocused", function (elementId, username) {
    console.log("FOCUSED " + elementId);
    var $el = $("#match_pane_" + elementId);
    
    if ($el.length) {
        // Disable the element
        $el.prop("disabled", true);
        
        // Add blur effect to the panel
        $el.addClass("focused-blur");
        
        // Add CSS styles if they don't exist
        if (!$("#focus-blur-styles").length) {
            var styles = $('<style id="focus-blur-styles">' +
                '.focused-blur {' +
                    'filter: blur(2px) !important;' +
                    'opacity: 0.6 !important;' +
                    'pointer-events: none !important;' +
                    'transition: all 0.3s ease !important;' +
                    'position: relative !important;' +
                '}' +
                '.focused-blur::before {' +
                    'content: "";' +
                    'position: absolute;' +
                    'top: 0;' +
                    'left: 0;' +
                    'right: 0;' +
                    'bottom: 0;' +
                    'background: rgba(0, 0, 0, 0.1);' +
                    'z-index: 1;' +
                    'pointer-events: none;' +
                '}' +
                '.tippy-box {' +
                    'z-index: 10000 !important;' +
                '}' +
                '</style>');
            $('head').append(styles);
        }
        
        // Handle tooltip with jQuery
        if (typeof tippy !== "undefined") {
            // Destroy existing tippy instance if it exists
            if ($el.data('tippy-instance')) {
                $el.data('tippy-instance').destroy();
                $el.removeData('tippy-instance');
            }
            
            // Create new tippy instance on the jQuery element
            var tippyInstance = tippy($el.get(0), {
                content: '<strong>' + username + '</strong> is editing',
                allowHTML: true,
                trigger: "manual",
                hideOnClick: false,
                interactive: false,
                arrow: true,
                placement: "top",
                theme: "light-border",
                duration: [0, 0],
                showOnCreate: false,
                appendTo: function() { return document.body; },
                zIndex: 10001,
                offset: [0, 10]
            });
            
            // Store reference using jQuery data
            $el.data('tippy-instance', tippyInstance);
            
            // Show the tooltip
            tippyInstance.show();
            
            console.log("Tooltip created and shown for:", username);
            
        } else {
            // jQuery fallback - create custom tooltip
            var tooltipId = 'custom-tooltip-' + elementId;
            
            // Remove existing custom tooltip
            $('#' + tooltipId).remove();
            
            // Get element position using jQuery
            var offset = $el.offset();
            var width = $el.outerWidth();
            var height = $el.outerHeight();
            
            // Create custom tooltip element with jQuery
            var $tooltip = $('<div/>', {
                id: tooltipId,
                text: username + ' upravuje',
                css: {
                    position: 'absolute',
                    top: (offset.top - 35) + 'px',
                    left: (offset.left + width/2) + 'px',
                    transform: 'translateX(-50%)',
                    background: 'rgba(0, 0, 0, 0.8)',
                    color: 'white',
                    padding: '5px 10px',
                    borderRadius: '4px',
                    fontSize: '12px',
                    zIndex: 10001,
                    pointerEvents: 'none',
                    whiteSpace: 'nowrap'
                }
            });
            
            $('body').append($tooltip);
            $el.data('custom-tooltip-id', tooltipId);
        }
        
        console.log("Applied blur and tooltip to element:", elementId);
    } else {
        console.warn("Element not found:", "match_pane_" + elementId);
    }
});

connection.on("ElementReleased", function (elementId) {
    console.log("RELEASED " + elementId);
    var $el = $("#match_pane_" + elementId);
    
    if ($el.length) {
        // Re-enable the element
        $el.prop("disabled", false);
        
        // Remove blur effect
        $el.removeClass("focused-blur");
        
        // Clean up tooltips using jQuery data
        var tippyInstance = $el.data('tippy-instance');
        if (tippyInstance) {
            tippyInstance.destroy();
            $el.removeData('tippy-instance');
        }
        
        var customTooltipId = $el.data('custom-tooltip-id');
        if (customTooltipId) {
            $('#' + customTooltipId).remove();
            $el.removeData('custom-tooltip-id');
        }
        
        // Remove title attribute as final cleanup
        $el.removeAttr("title");
        
        console.log("Removed blur and tooltip from element:", elementId);
    } else {
        console.warn("Element not found for release:", "match_pane_" + elementId);
    }
});
// Set up client-side event handlers for the methods your server will call
connection.on("AcceptChangeMatchAdd", function (matchId,refereeId, refereeName, role,user,timestampChange) {
    let matchDelegationPane;
    if (role === 0) {
        matchDelegationPane = "referee_delegation_pane_" + matchId;
    }
    else if (role === 1) {
        matchDelegationPane = "ar1_delegation_pane_" + matchId;
    }
    else if (role === 2) {
        matchDelegationPane = "ar2_delegation_pane_" + matchId;
    }
    const $paneDelegationWrapper = $("#" + matchDelegationPane);
    var $parentDiv = $paneDelegationWrapper.closest('.position-relative');


    const $button = $(`
        <button class="btn btn-secondary referee-button" data-id="${refereeId}"
            style="position: absolute;height:25px;width:80px;padding:0;display:flex;align-items: center;justify-content: center;flex-direction: row;margin-left: 30px;margin-top: 5px;">
            <strong>${refereeName}</strong>
        </button>
    `);

    $parentDiv.append($button);
     const whoChanged = "last_changed_by_"+ matchId;
    const $whoChangedDiv = $("#" + whoChanged);
    $whoChangedDiv.text(user);

    const whenChanged = "last_changed_"+ matchId;
    const $whenChangedDiv = $("#" + whenChanged);
    const date = new Date(timestampChange); 

	const day = String(date.getDate()).padStart(2, '0');
	const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are 0-indexed
	const hours = String(date.getHours()).padStart(2, '0');
	const minutes = String(date.getMinutes()).padStart(2, '0');
	const seconds = String(date.getSeconds()).padStart(2, '0');

     const formatted = `${day}/${month} ${hours}:${minutes}:${seconds}`;

     $whenChangedDiv.text(formatted);

});

connection.on("AcceptChangeMatchRemove", function (matchId, refereeId,user,timestampChange) {
    const matchPane = "match_pane_" + matchId;
    const $paneWrapper = $("#" + matchPane);

    $paneWrapper.find('button[data-id="' + refereeId + '"]').remove();
    const whoChanged = "last_changed_by_"+ matchId;
    const $whoChangedDiv = $("#" + whoChanged);
    $whoChangedDiv.text(user);

    const whenChanged = "last_changed_"+ matchId;
    const $whenChangedDiv = $("#" + whenChanged);

    const date = new Date(timestampChange);

        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are 0-indexed
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        const seconds = String(date.getSeconds()).padStart(2, '0');

     const formatted = `${day}/${month} ${hours}:${minutes}:${seconds}`;

     $whenChangedDiv.text(formatted);

});

connection.on("AcceptChangeReferee", function (data) {
    if (!data) {
    	console.error("Received undefined or null data");
    	return;
  	}

    if (!data.refereeId) {
    	console.error("Expected property 'refereeId' is missing:", data);
    	return;
    }

    if (!data.refereeData.Referee.Name) {
        console.error("Expected property 'name' is missing:", data.refereeData);
        return;
    }
    const buttonId = data.refereeId + "_referee-offer-button";
    const $buttonWrapper = $("#" + buttonId);

    if ($buttonWrapper.length) {
        $buttonWrapper.empty();


        // Add left side rectangles based on updated data
        if (data.refereeData.isFreeSaturdayMorning) {
            $buttonWrapper.append('<div class="rectangle rect-left-top" style="background-color: yellow;"></div>');
        }
        if (data.refereeData.isFreeSaturdayAfternoon) {
            $buttonWrapper.append('<div class="rectangle rect-left-bottom" style="background-color: darkblue;"></div>');
        }

        if (data.refereeData.isFreeSundayMorning) {
            $buttonWrapper.append('<div class="rectangle rect-right-top" style="background-color: darkred;"></div>');
        }
        if (data.refereeData.isFreeSundayAfternoon) {
            $buttonWrapper.append('<div class="rectangle rect-right-bottom" style="background-color: forestgreen;"></div>');
        }

        // Build button HTML
        const buttonHtml = `
            <button id="${data.refereeId}_referee-offer-pure-button" 
                    class="btn btn-secondary referee-button ${data.refereeData.hasSpecialNote ? 'sticky' : ''}" 
                    data-id="${data.refereeId}">
                ${data.refereeData.hasSpecialNote ? '<i class="fas fa-sticky-note fa-sm text-warning"></i>' : ''}
                <strong>${data.refereeData.Referee.Name.substring(0, 1)}. ${data.refereeData.Referee.Surname}</strong>
            </button>
        `;
	 $buttonWrapper.append(buttonHtml);

        $.ajax({
            url: 'Admin/Referee/UploadRefreshedMatch',
            type: 'POST',
            data: { matchId: data.refereeData.matchId },
            success: function (updatedMatches) {
            },
            error: function (xhr, status, error) {
                console.error("Failed to refresh match", error);
            }
        });
    }
    else {
        console.log("Button with ID " + buttonId + " not found");
    }
});
connection.on("AcceptMatchLockUpdate", function (matchId, lockStatus,user,timestampChange) {

    const matchPane = "match_pane_" + matchId;
    const $paneWrapper = $("#" + matchPane);

    if (!lockStatus) {
        $paneWrapper.find("input, select, button, textarea").not(".lock-button").prop("disabled", false);
        $paneWrapper.removeClass("disabled-pane");
    } else {
        $paneWrapper.find("input, select, button, textarea").not(".lock-button").prop("disabled", true);
        $paneWrapper.addClass("disabled-pane");
    }

    const whoChanged = "last_changed_by_"+ matchId;
    const $whoChangedDiv = $("#" + whoChanged);
    $whoChangedDiv.text(user);

    const whenChanged = "last_changed_"+ matchId;
    const $whenChangedDiv = $("#" + whenChanged);
    const date = new Date(timestampChange);

        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are 0-indexed
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        const seconds = String(date.getSeconds()).padStart(2, '0');

     const formatted = `${day}/${month} ${hours}:${minutes}:${seconds}`;

     $whenChangedDiv.text(formatted);
});
startConnection();
