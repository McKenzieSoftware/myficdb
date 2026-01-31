// Updating the year in the footer since we're using Roman Numerals
// https://stackoverflow.com/a/9083076
function romanize(num) {
    if (isNaN(num))
        return NaN;
    var digits = String(+num).split(""),
        key = ["", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM",
            "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC",
            "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX"],
        roman = "",
        i = 3;
    while (i--)
        roman = (key[+digits.pop() + (i * 10)] || "") + roman;
    return Array(+digits.join("") + 1).join("M") + roman;
}

// Update the year in the footer using the above function
const currentYear = new Date().getFullYear();

const currentEl = document.getElementById('f-current-year');
currentEl.innerText = romanize(currentYear);
currentEl.dataset.bsTitle = currentYear;

const startEl = document.getElementById('f-start-year');
startEl.innerText = romanize(2025);
startEl.dataset.bsTitle = 2025;