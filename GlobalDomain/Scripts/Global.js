function loadPartialView(selector, url, params, onSuccess) {
    $.ajax({
        url: url,
        type: 'GET',
        data: params || {},
        success: function (response) {
            $(selector).html(response);
            if (onSuccess && typeof onSuccess === 'function') {
                onSuccess(response);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading partial view:', error);
        }
    });
}