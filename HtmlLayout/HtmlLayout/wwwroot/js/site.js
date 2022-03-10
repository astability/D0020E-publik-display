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
            var scroll = document.getElementById('scrollImportant');
            if (content[0].style.display === "block") {
                scroll.style.display = 'none';
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'none';
                }
            } else {
                scroll.style.display = 'block';
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'block';
                }
            }
        });
    } else if (i == 1){
        coll[i].addEventListener("click", function () {
            this.classList.toggle("active");
            var content = document.getElementsByClassName('contentSolved');
            var scroll = document.getElementById('scrollSolved');
            if (content[0].style.display === "block") {
                scroll.style.display = 'none';
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'none';
                }
            } else {
                scroll.style.display = 'block';
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'block';
                }
            }
        });
    } else if (i == 2) {
        coll[i].addEventListener("click", function () {
            this.classList.toggle("active");
            var content = document.getElementsByClassName('contentVerbose');
            var scroll = document.getElementById('scrollVerbose');
            if (content[0].style.display === "block") {
                scroll.style.display = 'none';
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'none';
                }
            } else {
                scroll.style.display = 'block';
                for (j = 0; j < content.length; j++) {
                    content[j].style.display = 'block';
                }
            }
        });
    }
}

idleCheck();

function idleCheck() {
    var t;
    window.onload = resetTimer;
    window.onmousedown = resetTimer;
    window.ontouchstart = resetTimer;
    window.ontouchmove = resetTimer;
    window.onclick = resetTimer;
    window.onkeydown = resetTimer;
    window.addEventListener('scroll', resetTimer, true);

    function reload() {
        document.getElementById('logoutBtn').click();
    }

    function resetTimer() {
        clearTimeout(t);
        t = setTimeout(reload, 10000);  // milliseconds
    }
}