window.scrollToBottom = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) {
        el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    }
};
