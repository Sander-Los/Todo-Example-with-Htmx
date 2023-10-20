$("#js-todoForm").on("htmx:afterRequest", function() {
    $("#js-todoForm input").val('');
})