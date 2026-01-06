export function beforeWebStart(options, extensions) {
	beforeStart(options, extensions);
}

export function beforeStart(options, extensions) {
	var element = document.createElement('link');
	element.href = "_content/Kebechet.Blazor.Icons.Tabler/tabler-icons.min.css";
	element.rel = "stylesheet";
	element.async = false;
	document.head.appendChild(element);
}
