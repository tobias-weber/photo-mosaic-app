import {Component, effect, ElementRef, inject, input, NgZone, OnDestroy, OnInit, signal} from '@angular/core';
import OpenSeadragon, {ControlAnchor, Viewer} from 'openseadragon';
import {ProjectService} from '../../../services/project.service';
import {tokenKey} from '../../../services/auth.service';
import {ApiService} from '../../../services/api.service';
import {FormsModule} from '@angular/forms';

@Component({
    selector: 'app-mosaic-viewer',
    imports: [
        FormsModule
    ],
    templateUrl: './mosaic-viewer.component.html',
    styleUrl: './mosaic-viewer.component.css'
})
export class MosaicViewerComponent implements OnInit, OnDestroy {
    private host = inject(ElementRef<HTMLElement>);
    private ngZone = inject(NgZone);
    private project = inject(ProjectService);
    private api = inject(ApiService);
    viewer?: Viewer;

    // input from parent (e.g. jobId)
    jobId = input.required<string>();
    overlayUrl = input<string>();
    hasOverlay = false;
    opacity = signal(0);

    constructor() {
        effect(() => {
            if (this.overlayUrl() && this.viewer && this.opacity() > 0 && !this.hasOverlay) {
                // by waiting for the opacity to change from user input we ensure the image has time to load
                const bounds = this.viewer.world.getHomeBounds();
                this.viewer.addOverlay(
                    'image-overlay',
                    new OpenSeadragon.Rect(0, 0, bounds.width, bounds.height)
                );
                this.hasOverlay = true;
            }
        });
    }

    ngOnInit() {
        const url = this.api.constructDzUrl(this.project.targetUser(), this.project.projectId()!, this.jobId());

        // Currently required as a workaround for the issue https://github.com/openseadragon/openseadragon/issues/2756
        const ts = [
            {
                tileSource: url,
                ajaxHeaders: {
                    'Authorization': `Bearer ${localStorage.getItem(tokenKey)}`
                }
            }
        ];

        const options = {
            element: this.host.nativeElement.querySelector('div')!,
            tileSources: ts,
            prefixUrl: '//openseadragon.github.io/openseadragon/images/', // where OSD button images live
            minZoomImageRatio: 0.25,
            maxZoomPixelRatio: 2,
            defaultZoomLevel: 0,
            showNavigator: true,
            ajaxHeaders: {
                Authorization: `Bearer ${localStorage.getItem(tokenKey)}`
            },
            loadTilesWithAjax: true,
            navigatorSizeRatio: 0.15,
            navigatorDisplayRegionColor: '#dc3545',
            autoHideControls: true,
            navigationControlAnchor: ControlAnchor.BOTTOM_RIGHT
        }

        // @ts-ignore
        this.viewer = this.ngZone.runOutsideAngular(() => OpenSeadragon(options));


    }


    ngOnDestroy() {
        this.viewer?.destroy();
    }

}
