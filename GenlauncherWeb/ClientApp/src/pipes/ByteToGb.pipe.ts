import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  standalone: true,
  name: 'byteToGb'
})
export class ByteToGbPipe implements PipeTransform {

  transform(value: number, decimalPoints: number = 2): string {
    if (isNaN(value)) {
      return null;
    }

    const bytesInMB = 1024 ** 3;
    const megabytes = value / bytesInMB;

    return megabytes.toFixed(decimalPoints) + ' GB';
  }

}
