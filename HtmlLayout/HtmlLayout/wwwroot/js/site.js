// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Systeminfo logs 
var coll = document.getElementsByClassName("collapsible");
var i;
var j;

for (i = 0; i < coll.length; i++) {
    if (i == 0) {
        coll[i].addEventListener("click", function () {
            this.classList.toggle("active");
            var content = document.getElementsByClassName('contentImportant');
            if (content[0].style.display === "block") {
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'none';
                }
            } else {
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'block';
                }
            }
        });
    } else {
        coll[i].addEventListener("click", function () {
            this.classList.toggle("active");
            var content = document.getElementsByClassName('contentVerbose');
            if (content[0].style.display === "block") {
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'none';
                }
            } else {
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'block';
                }
            }
        });
    }
}
