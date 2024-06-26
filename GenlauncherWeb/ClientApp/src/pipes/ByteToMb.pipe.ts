import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  standalone: true,
  name: 'byteToMb'
})
export class ByteToMbPipe implements PipeTransform {

  transform(value: number, decimalPoints: number = 2): string {
    if (isNaN(value)) {
      return null;
    }

    const bytesInMB = 1024 ** 2;
    const megabytes = value / bytesInMB;

    return megabytes.toFixed(decimalPoints) + ' MB';
  }

}
