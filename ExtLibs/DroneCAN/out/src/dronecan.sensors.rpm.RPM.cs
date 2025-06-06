
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;

using int8_t = System.SByte;
using int16_t = System.Int16;
using int32_t = System.Int32;
using int64_t = System.Int64;

using float32 = System.Single;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace DroneCAN
{
    public partial class DroneCAN {

        public partial class dronecan_sensors_rpm_RPM : IDroneCANSerialize
        {
            public static void encode_dronecan_sensors_rpm_RPM(dronecan_sensors_rpm_RPM msg, dronecan_serializer_chunk_cb_ptr_t chunk_cb, object ctx, bool fdcan) {
                uint8_t[] buffer = new uint8_t[8];
                _encode_dronecan_sensors_rpm_RPM(buffer, msg, chunk_cb, ctx, !fdcan);
            }

            public static uint32_t decode_dronecan_sensors_rpm_RPM(CanardRxTransfer transfer, dronecan_sensors_rpm_RPM msg, bool fdcan) {
                uint32_t bit_ofs = 0;
                _decode_dronecan_sensors_rpm_RPM(transfer, ref bit_ofs, msg, !fdcan);
                return (bit_ofs+7)/8;
            }

            internal static void _encode_dronecan_sensors_rpm_RPM(uint8_t[] buffer, dronecan_sensors_rpm_RPM msg, dronecan_serializer_chunk_cb_ptr_t chunk_cb, object ctx, bool tao) {
                memset(buffer,0,8);
                canardEncodeScalar(buffer, 0, 8, msg.sensor_id);
                chunk_cb(buffer, 8, ctx);
                memset(buffer,0,8);
                canardEncodeScalar(buffer, 0, 16, msg.flags);
                chunk_cb(buffer, 16, ctx);
                memset(buffer,0,8);
                canardEncodeScalar(buffer, 0, 32, msg.rpm);
                chunk_cb(buffer, 32, ctx);
            }

            internal static void _decode_dronecan_sensors_rpm_RPM(CanardRxTransfer transfer,ref uint32_t bit_ofs, dronecan_sensors_rpm_RPM msg, bool tao) {

                canardDecodeScalar(transfer, bit_ofs, 8, false, ref msg.sensor_id);
                bit_ofs += 8;

                canardDecodeScalar(transfer, bit_ofs, 16, false, ref msg.flags);
                bit_ofs += 16;

                canardDecodeScalar(transfer, bit_ofs, 32, true, ref msg.rpm);
                bit_ofs += 32;

            }
        }
    }
}