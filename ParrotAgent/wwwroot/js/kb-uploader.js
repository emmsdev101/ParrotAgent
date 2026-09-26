/* =========================================================
   ParrotAgent — Knowledge Base Uploader (Axios)
   wwwroot/js/kb-uploader.js
   ========================================================= */
(function () {
    'use strict';

    // ---------------------------------------------------------
    // Config
    // ---------------------------------------------------------
    
    const url = window.location.pathname;

    // Get path segments without empty strings caused by trailing slashes
    const segments = url.split('/').filter(Boolean); 
    const id = segments[segments.length - 1];

    var UPLOAD_URL    = '/api/knowledge-base/'+id+'/upload-document';
    var MAX_FILE_SIZE = 100 * 1024 * 1024;   // 100 MB
    var CONCURRENCY   = 3;

    // ---------------------------------------------------------
    // DOM refs
    // ---------------------------------------------------------
    var dropzone       = document.querySelector('[data-kbv-dropzone]');
    var fileInput      = document.querySelector('[data-kbv-file-input]');
    var browseBtn      = document.querySelector('[data-kbv-browse]');
    var uploadTrigger  = document.querySelector('[data-kbv-upload-trigger]');
    var uploadsSection = document.querySelector('[data-kbv-uploads]');
    var uploadList     = document.querySelector('[data-kbv-upload-list]');
    var totalFill      = document.querySelector('[data-kbv-total-fill]');
    var summaryTitle   = document.querySelector('[data-kbv-summary-title]');
    var summarySub     = document.querySelector('[data-kbv-summary-sub]');
    var cancelAllBtn   = document.querySelector('[data-kbv-cancel-all]');
    var clearDoneBtn   = document.querySelector('[data-kbv-clear-done]');
    var itemTemplate   = document.getElementById('kbv-upload-item-template');

    if (!dropzone || !uploadList || !itemTemplate || !uploadsSection) return;
    if (typeof axios === 'undefined') {
        console.error('[kb-uploader] Axios is not loaded.');
        return;
    }

    // ---------------------------------------------------------
    // Axios instance — with XSRF cookie support
    // ---------------------------------------------------------
    var api = axios.create({
        baseURL: '',
        timeout: 0,   // no timeout — big files take a while
        withCredentials: true
    });

    // If you're using ASP.NET Core antiforgery, add the header globally.
    // (Only needed if your endpoint requires it — file uploads via
    //  multipart usually aren't protected by antiforgery.)
    var xsrfToken = getCookie('.AspNetCore.Antiforgery');
    if (xsrfToken) {
        api.defaults.headers.common['RequestVerificationToken'] = xsrfToken;
    }

    function getCookie(name) {
        var match = document.cookie.match(new RegExp('(^|;\\s*)' + name + '=([^;]*)'));
        return match ? decodeURIComponent(match[2]) : null;
    }

    // ---------------------------------------------------------
    // State
    // ---------------------------------------------------------
    var uploads = [];      // { id, file, cancel, status, loaded, total, ... }
    var activeCount = 0;
    var nextId = 1;

    // ---------------------------------------------------------
    // Icon by file type
    // ---------------------------------------------------------
    function iconForFile(name) {
        var ext = (name.split('.').pop() || '').toLowerCase();
        switch (ext) {
            case 'pdf':              return { glyph: 'fa-file-pdf',        cls: 'pdf' };
            case 'doc': case 'docx': return { glyph: 'fa-file-word',       cls: 'docx' };
            case 'xls': case 'xlsx': return { glyph: 'fa-file-excel',      cls: 'xlsx' };
            case 'ppt': case 'pptx': return { glyph: 'fa-file-powerpoint', cls: 'pptx' };
            case 'md':  case 'txt':  return { glyph: 'fa-file-code',       cls: 'md' };
            case 'csv':              return { glyph: 'fa-file-csv',        cls: 'xlsx' };
            default:                 return { glyph: 'fa-file',            cls: 'default' };
        }
    }

    function formatSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
        return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
    }

    // ---------------------------------------------------------
    // Add files
    // ---------------------------------------------------------
    function addFiles(fileList) {
        var files = Array.prototype.slice.call(fileList || []);
        if (!files.length) return;

        uploadsSection.hidden = false;

        files.forEach(function (file) {
            if (file.size > MAX_FILE_SIZE) {
                addRejectedItem(file, 'File exceeds ' + formatSize(MAX_FILE_SIZE));
                return;
            }

            var item = {
                id: nextId++,
                file: file,
                cancel: null,          // axios cancel token source
                status: 'queued',
                loaded: 0,
                total: file.size
            };
            uploads.push(item);
            renderItem(item);
        });

        updateSummary();
        pump();
    }

    function addRejectedItem(file, reason) {
        var item = {
            id: nextId++,
            file: file,
            cancel: null,
            status: 'error',
            error: reason,
            loaded: 0,
            total: file.size
        };
        uploads.push(item);
        renderItem(item);
        setItemStatus(item, 'error', reason);
    }

    // ---------------------------------------------------------
    // Render an item
    // ---------------------------------------------------------
    function renderItem(item) {
        var node = itemTemplate.content.firstElementChild.cloneNode(true);

        item.node = node;
        item.elIcon      = node.querySelector('[data-upload-icon]');
        item.elIconGlyph = node.querySelector('[data-upload-icon-glyph]');
        item.elName      = node.querySelector('[data-upload-name]');
        item.elSize      = node.querySelector('[data-upload-size]');
        item.elBar       = node.querySelector('[data-upload-bar]');
        item.elPct       = node.querySelector('[data-upload-progress-text]');
        item.elStatus    = node.querySelector('[data-upload-status]');
        item.elCancel    = node.querySelector('[data-upload-cancel]');
        item.elRetry     = node.querySelector('[data-upload-retry]');
        item.elRemove    = node.querySelector('[data-upload-remove]');

        var icon = iconForFile(item.file.name);
        item.elIconGlyph.className = 'fas ' + icon.glyph;
        if (icon.cls !== 'default') item.elIcon.classList.add(icon.cls);

        item.elName.textContent = item.file.name;
        item.elSize.textContent = formatSize(item.file.size);
        item.elBar.style.width = '0%';
        item.elPct.textContent = '0%';

        item.elCancel.addEventListener('click', function () { cancelItem(item); });
        item.elRetry.addEventListener('click',  function () { retryItem(item); });
        item.elRemove.addEventListener('click', function () { removeItem(item); });

        setItemStatus(item, 'queued');
        uploadList.appendChild(node);
    }

    // ---------------------------------------------------------
    // Status
    // ---------------------------------------------------------
    function setItemStatus(item, status, message) {
        item.status = status;
        var node = item.node;
        if (!node) return;

        node.classList.remove('is-uploading', 'is-done', 'is-error', 'is-cancelled');

        item.elCancel.hidden = true;
        item.elRetry.hidden  = true;
        item.elRemove.hidden = true;

        item.elStatus.textContent = '';
        item.elStatus.className = 'kbv-upload-status';

        switch (status) {
            case 'queued':
                item.elStatus.textContent = 'Queued';
                item.elCancel.hidden = false;
                break;
            case 'uploading':
                node.classList.add('is-uploading');
                item.elStatus.textContent = 'Uploading';
                item.elCancel.hidden = false;
                break;
            case 'done':
                node.classList.add('is-done');
                item.elStatus.textContent = 'Completed';
                item.elStatus.classList.add('done');
                item.elBar.style.width = '100%';
                item.elPct.textContent = '100%';
                item.elRemove.hidden = false;
                break;
            case 'error':
                node.classList.add('is-error');
                item.elStatus.textContent = message || 'Failed';
                item.elStatus.classList.add('error');
                item.elRetry.hidden = false;
                item.elRemove.hidden = false;
                break;
            case 'cancelled':
                node.classList.add('is-cancelled');
                item.elStatus.textContent = 'Cancelled';
                item.elStatus.classList.add('cancelled');
                item.elRemove.hidden = false;
                break;
        }
    }

    // ---------------------------------------------------------
    // Concurrency pump
    // ---------------------------------------------------------
    function pump() {
        var free = CONCURRENCY - activeCount;
        if (free <= 0) return;

        uploads.filter(function (u) { return u.status === 'queued'; })
               .slice(0, free)
               .forEach(startUpload);
    }

    // ---------------------------------------------------------
    // Upload one file with axios
    // ---------------------------------------------------------
    function startUpload(item) {
        if (item.status !== 'queued') return;

        activeCount++;
        setItemStatus(item, 'uploading');

        var form = new FormData();
        form.append('file', item.file, item.file.name);

        var kbIdEl = document.querySelector('[data-kb-id]');
        if (kbIdEl) {
            var kbId = kbIdEl.dataset.kbId || kbIdEl.textContent.trim();
            if (kbId) form.append('knowledgeBaseId', kbId);
        }

        var source = axios.CancelToken.source();
        item.cancel = source;

        api.post(UPLOAD_URL, form, {
            cancelToken: source.token,
            onUploadProgress: function (e) {
                if (!e.lengthComputable && !e.total) return;
                item.loaded = e.loaded;
                item.total  = e.total || item.file.size;
                var pct = Math.round((e.loaded / (e.total || item.file.size)) * 100);
                item.elBar.style.width = pct + '%';
                item.elPct.textContent = pct + '%';
                updateSummary();
            }
        })
        .then(function (response) {
            activeCount--;
            item.loaded = item.total;
            setItemStatus(item, 'done');
            updateSummary();
            pump();
            return response;
        })
        .catch(function (error) {
            activeCount--;

            if (axios.isCancel(error)) {
                setItemStatus(item, 'cancelled');
            } else if (error.response) {
                var msg = (error.response.data && (error.response.data.message || error.response.data.error))
                    || ('Upload failed (' + error.response.status + ')');
                setItemStatus(item, 'error', msg);
            } else if (error.request) {
                setItemStatus(item, 'error', 'Network error');
            } else {
                setItemStatus(item, 'error', error.message || 'Unknown error');
            }

            updateSummary();
            pump();
        });
    }

    // ---------------------------------------------------------
    // Actions
    // ---------------------------------------------------------
    function cancelItem(item) {
        if (item.cancel && item.status === 'uploading') {
            item.cancel.cancel('Cancelled by user');
        } else if (item.status === 'queued') {
            setItemStatus(item, 'cancelled');
            updateSummary();
        }
    }

    function retryItem(item) {
        if (item.status !== 'error') return;
        item.loaded = 0;
        item.elBar.style.width = '0%';
        item.elPct.textContent = '0%';
        setItemStatus(item, 'queued');
        updateSummary();
        pump();
    }

    function removeItem(item) {
        if (item.node && item.node.parentNode) {
            item.node.parentNode.removeChild(item.node);
        }
        uploads = uploads.filter(function (u) { return u !== item; });
        updateSummary();
    }

    // ---------------------------------------------------------
    // Cancel all / clear completed
    // ---------------------------------------------------------
    if (cancelAllBtn) {
        cancelAllBtn.addEventListener('click', function () {
            uploads.forEach(function (item) {
                if (item.status === 'uploading' && item.cancel) {
                    item.cancel.cancel('Cancelled by user');
                } else if (item.status === 'queued') {
                    setItemStatus(item, 'cancelled');
                }
            });
            updateSummary();
        });
    }

    if (clearDoneBtn) {
        clearDoneBtn.addEventListener('click', function () {
            uploads.filter(function (u) { return u.status === 'done'; })
                   .forEach(removeItem);
        });
    }

    // ---------------------------------------------------------
    // Summary
    // ---------------------------------------------------------
    function updateSummary() {
        if (!uploads.length) {
            uploadsSection.hidden = true;
            return;
        }

        var totalBytes  = uploads.reduce(function (s, u) { return s + (u.total  || 0); }, 0);
        var loadedBytes = uploads.reduce(function (s, u) { return s + (u.loaded || 0); }, 0);
        var overallPct  = totalBytes ? Math.round((loadedBytes / totalBytes) * 100) : 0;

        totalFill.style.width = overallPct + '%';

        var uploading = uploads.filter(function (u) { return u.status === 'uploading'; }).length;
        var queued    = uploads.filter(function (u) { return u.status === 'queued'; }).length;
        var done      = uploads.filter(function (u) { return u.status === 'done'; }).length;
        var errors    = uploads.filter(function (u) { return u.status === 'error'; }).length;

        if (uploading > 0) {
            summaryTitle.textContent = 'Uploading ' + (uploading + queued) + ' file' +
                ((uploading + queued) === 1 ? '' : 's') + '...';
        } else if (errors > 0) {
            summaryTitle.textContent = done + ' uploaded, ' + errors + ' failed';
        } else if (done > 0) {
            summaryTitle.textContent = 'All uploads complete (' + done + ')';
        } else {
            summaryTitle.textContent = 'Ready to upload';
        }

        summarySub.textContent =
            formatSize(loadedBytes) + ' of ' + formatSize(totalBytes) +
            ' · ' + overallPct + '%';

        if (clearDoneBtn) {
            clearDoneBtn.hidden = done === 0 || uploading > 0;
        }
    }

    // =========================================================
    // Drag & drop
    // =========================================================
    ['dragenter', 'dragover'].forEach(function (evt) {
        dropzone.addEventListener(evt, function (e) {
            e.preventDefault();
            e.stopPropagation();
            dropzone.classList.add('dragover');
        });
    });

    ['dragleave', 'drop'].forEach(function (evt) {
        dropzone.addEventListener(evt, function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (evt === 'dragleave') {
                if (e.target === dropzone) dropzone.classList.remove('dragover');
            } else {
                dropzone.classList.remove('dragover');
            }
        });
    });

    dropzone.addEventListener('drop', function (e) {
        var dt = e.dataTransfer;
        if (dt && dt.files && dt.files.length) addFiles(dt.files);
    });

    ['dragover', 'drop'].forEach(function (evt) {
        window.addEventListener(evt, function (e) {
            if (!e.target.closest('[data-kbv-dropzone]')) e.preventDefault();
        });
    });

    // =========================================================
    // Click to browse
    // =========================================================
    dropzone.addEventListener('click', function (e) {
        if (e.target.closest('[data-kbv-browse]') || e.target === dropzone) {
            fileInput.click();
        }
    });

    if (browseBtn) {
        browseBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            fileInput.click();
        });
    }

    if (uploadTrigger) {
        uploadTrigger.addEventListener('click', function () {
            fileInput.click();
        });
    }

    fileInput.addEventListener('change', function () {
        if (fileInput.files.length) {
            addFiles(fileInput.files);
            fileInput.value = '';
        }
    });

})();