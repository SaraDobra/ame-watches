import { Pipe, PipeTransform } from '@angular/core';
import { imageSrc } from '../util/image';

@Pipe({ name: 'appImg' })
export class ImgPipe implements PipeTransform {
  transform(url: string | null | undefined): string {
    return imageSrc(url);
  }
}
