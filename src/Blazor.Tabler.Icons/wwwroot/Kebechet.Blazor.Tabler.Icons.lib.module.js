export function beforeWebStart(options, extensions) {
	beforeStart(options, extensions);
}

export function beforeStart(options, extensions) {
	var outline = document.createElement('link');
	outline.href = "_content/Kebechet.Blazor.Tabler.Icons/tabler-icons.min.css";
	outline.rel = "stylesheet";
	outline.async = false;
	document.head.appendChild(outline);

	var filled = document.createElement('link');
	filled.href = "_content/Kebechet.Blazor.Tabler.Icons/tabler-icons-filled.min.css";
	filled.rel = "stylesheet";
	filled.async = false;
	document.head.appendChild(filled);
}
