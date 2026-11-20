// Section reveal on scroll for the portfolio landing page.
//
// Adds .js-reveal to the page root (so the CSS only hides content when script
// is actually running) and marks each wave/section .is-revealed as it enters the
// viewport. Elements above the fold are revealed immediately so the first paint
// is never blank.
window.portfolioReveal = (function () {
    "use strict";

    var observer = null;

    function attach() {
        var root = document.querySelector(".landing");
        if (!root || root.classList.contains("js-reveal")) {
            return;
        }

        // Opt out entirely when the visitor prefers reduced motion: leave the
        // markup visible rather than animating it.
        var reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (reduced || !("IntersectionObserver" in window)) {
            return;
        }

        var targets = root.querySelectorAll(".section-wave, .section");
        if (!targets.length) {
            return;
        }

        root.classList.add("js-reveal");

        observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) {
                    return;
                }

                entry.target.classList.add("is-revealed");
                observer.unobserve(entry.target);
            });
        }, {
            // Trigger a little before the section is fully in view, and require
            // some overlap so a section taller than the viewport still reveals.
            rootMargin: "0px 0px -12% 0px",
            threshold: 0.05
        });

        targets.forEach(function (target) {
            // Anything already on screen at load should not fade in late.
            var rect = target.getBoundingClientRect();
            if (rect.top < window.innerHeight) {
                target.classList.add("is-revealed");
                return;
            }

            observer.observe(target);
        });
    }

    return { attach: attach };
})();