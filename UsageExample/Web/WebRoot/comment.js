const openModal = document.getElementById('openModal');
const modal = new RModal(document.getElementById('modal'));

const modalSave = document.getElementById('modalFooterSave');
const modalClose = document.getElementById('modalFooterClose');

const commentsUl = document.getElementById('commentsUl');

const comments = JSON.parse(window.sessionStorage.getItem('comments') || '[]');

const commentField = document.getElementById('comment');

const getCookie = (name) => {
    const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    if (match) {
        return match[2];
    }
};

const renderComments = () => {
    commentsUl.innerText = '';

    comments.map(c => {
        
        const newLi = document.createElement('li');
        newLi.innerText = `${c.username} wrote:
        ${c.comment}`;
        commentsUl.appendChild(newLi);
    })
};

openModal.addEventListener('click', (e) => {
    modal.open();
});

modalSave.addEventListener('click', (e) => {
    const username = getCookie('username');
    comments.push({username: username, comment: commentField.value});
    window.sessionStorage.setItem('comments', JSON.stringify(comments));
    renderComments();

    commentField.value = '';
    modal.close();
});

modalClose.addEventListener('click', (e) => {
    commentField.value = '';
    modal.close();
});

document.addEventListener("DOMContentLoaded", renderComments);