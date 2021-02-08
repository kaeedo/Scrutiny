const logout = document.getElementById('logout');

logout.addEventListener('click', (e) => {
    document.cookie =`username=; expires=Thu, 01 Jan 1970 00:00:00 UTC;`;
    window.location = '/home';
});
