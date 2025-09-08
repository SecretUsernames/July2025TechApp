// File download functionality for PDF generation
window.downloadFile = (fileName, contentType, content) => {
    try {
        // Convert base64 string to bytes if needed
        let byteArray;
        
        if (typeof content === 'string') {
            // If content is a base64 string, decode it
            const byteCharacters = atob(content);
            const byteNumbers = new Array(byteCharacters.length);
            
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            
            byteArray = new Uint8Array(byteNumbers);
        } else {
            // If content is already a byte array
            byteArray = new Uint8Array(content);
        }
        
        // Create blob
        const blob = new Blob([byteArray], { type: contentType });
        
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.style.display = 'none';
        
        // Trigger download
        document.body.appendChild(link);
        link.click();
        
        // Cleanup
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        
        console.log(`Successfully downloaded file: ${fileName}`);
    } catch (error) {
        console.error('Error downloading file:', error);
        alert('Error downloading file. Please try again.');
    }
};