document.addEventListener("DOMContentLoaded", function () {
    const qrDataElement = document.getElementById("qrCodeData");
    const qrCodeContainer = document.getElementById("qrCode");

    if (qrDataElement && qrCodeContainer) {
        const url = qrDataElement.getAttribute("data-url");
        if (url) {
            new QRCode(qrCodeContainer, {
                text: url,
                width: 200,
                height: 200,
            });
        }
    }
});
