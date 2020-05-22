const form = document.getElementById('signInForm');
const username = document.getElementById('username');
const errorMessage = document.getElementById('ErrorMessage');
const numberInput = document.getElementById('number');

const numbersOnly = /^\d+$/;

form.addEventListener('submit', (e) => {
    const hasUsername = !!username.value;
    const hasNumber = numbersOnly.test(numberInput.value);

    if (hasUsername && hasNumber) {
        document.cookie =`username=${username.value};`;
        window.location = '/home';
    } else {
        form.style.border = 'red 1px solid';
        errorMessage.style.display = 'block';
        document.cookie =`username=; expires=Thu, 01 Jan 1970 00:00:00 UTC;`;
    }
});

numberInput.onchange = () => {
    form.style.border = 'none';
    errorMessage.style.display = 'none';
};

username.onchange = () => {
    form.style.border = 'none';
    errorMessage.style.display = 'none';
};