export function downloadImage(imageData, filename) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = imageData;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

export function shareOnTwitter(text/*, url*/) {
    //var twitterUrl = `https://twitter.com/intent/tweet?text=${encodeURIComponent(text)}&url=${encodeURIComponent(url)}`;
    var twitterUrl = `https://twitter.com/intent/tweet?text=${encodeURIComponent(text)}`;
    window.open(twitterUrl, '_blank');
}