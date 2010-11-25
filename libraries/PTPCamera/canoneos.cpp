#include "canoneos.h"


uint32_t ImgQualitySupplier::GetDataSize()
{
	return ((pictFormat & 0xFFFF0000) ? 0x0000002C : 0x0000001C);
}

void ImgQualitySupplier::GetData(const uint16_t len, uint8_t *pbuf)
{
	uint8_t		num_files = (pictFormat & 0xFFFF0000) ? 2 : 1;

	((uint32_t*)pbuf)[0] =  (num_files == 2) ? 0x0000002C : 0x0000001C;
	((uint32_t*)pbuf)[1] =	(uint32_t) EOS_DPC_ImageQuality;
	((uint32_t*)pbuf)[2] =	(uint32_t) num_files;

	uint32_t	format = pictFormat;
	
	for (uint8_t i=0, pos=3; i<num_files; i++)
	{
		((uint32_t*)pbuf)[pos++] = 0x00000010;

		for (uint8_t j=0; j<3; j++, format >>= 4)
			((uint32_t*)pbuf)[pos++] = (uint32_t)(format & 0xF);
	}
}

CanonEOS::CanonEOS(uint8_t addr, uint8_t epin, uint8_t epout, uint8_t epint, uint8_t nconf, PTPMAIN pfunc)
: PTP(addr, epin, epout, epint, nconf, pfunc)
{
}

uint16_t CanonEOS::SetImageQuality(uint32_t format)
{
	uint16_t	ptp_error	= PTP_RC_GeneralError;
	OperFlags	flags		= { 0, 0, 1, 1, 1, 0 };

	ImgQualitySupplier		sup;
	sup.SetPictureFormat(format);

	if ( (ptp_error = Transaction(PTP_OC_EOS_SetDevicePropValue, &flags, NULL, (void*)&sup)) != PTP_RC_OK)
		Message(PSTR("SetImageQuality error"), ptp_error);

	return ptp_error;
}

uint16_t CanonEOS::SetPCConnectMode(uint8_t mode)
{
	uint32_t	params[1];
	params[0] = (uint32_t) mode;
	return Operation(PTP_OC_EOS_SetPCConnectMode, 1, params);
}

uint16_t CanonEOS::SetExtendedEventInfo(uint8_t mode)
{
	uint32_t	params[1];
	params[0] = (uint32_t) mode;
	return Operation(PTP_OC_EOS_SetExtendedEventInfo, 1, params);
}

uint16_t CanonEOS::Initialize(bool binit)
{
	uint16_t	result1 = PTP_RC_OK, result2 = PTP_RC_OK;

	if (binit)
	{
		result2 = SetExtendedEventInfo(1);
		result1 = SetPCConnectMode(1);
	}
	else
	{
		result1 = SetPCConnectMode(0);
		result2 = SetExtendedEventInfo(0);
	}
	return (((result1 == PTP_RC_OK) && (result2 == PTP_RC_OK)) ? PTP_RC_OK : PTP_RC_GeneralError);
}

uint16_t CanonEOS::StartBulb()
{
	uint32_t	params[3];

	params[0] = 0xfffffff8;
	params[1] = 0x00001000;
	params[2] = 0x00000000;

	Operation(0x911A, 3, params);
	Operation(0x911B, 0, NULL);
	Operation(0x9125, 0, NULL);

	return PTP_RC_OK;
}

uint16_t CanonEOS::StopBulb()
{
	uint32_t	params[3];

    params[0] = 0xffffffff;
	params[1] = 0x00001000;
	params[2] = 0x00000000;
	Operation(0x911A, 3, params);
    
    params[0] = 0xfffffffc;
	Operation(0x911A, 3, params);
    
	Operation(0x9126, 0, NULL);
    delay(50);
	Operation(0x911C, 0, NULL);
    delay(50);
}

uint16_t CanonEOS::SwitchLiveView(bool on)
{
	uint16_t	ptp_error = PTP_RC_GeneralError;

	if ((ptp_error = SetProperty(EOS_DPC_LiveView, (on) ? 2 : 0)) == PTP_RC_OK)
	{
		if (on)
		{
			if ((ptp_error = SetProperty(0xD1B3, 0)) != PTP_RC_OK)
			{
				Message(PSTR("LiveView start failure:"), ptp_error);
				SetProperty(EOS_DPC_LiveView, 0);
				return PTP_RC_GeneralError;
			}
		}
	}
	return ptp_error;
}

uint16_t CanonEOS::MoveFocus(uint16_t step)
{
	uint16_t	ptp_error	= PTP_RC_GeneralError;
	OperFlags	flags		= { 1, 0, 0, 0, 0, 0 };
	uint32_t	params[1];

	params[0] = (uint32_t) step;

	if ( (ptp_error = Transaction(PTP_OC_EOS_MoveFocus, &flags, params, NULL)) != PTP_RC_OK)
		Message(PSTR("MoveFocus error."), ptp_error);
	else
		Message(PSTR("MoveFocus: Success."), ptp_error);

	return ptp_error;
}

uint16_t CanonEOS::EventCheck(PTPReadParser *parser)
{
	uint16_t	ptp_error	= PTP_RC_GeneralError;
	OperFlags	flags		= { 0, 0, 0, 1, 1, 0 };

	if ( (ptp_error = Transaction(0x9116, &flags, NULL, parser)) != PTP_RC_OK)
		Message(PSTR("EOSEventCheck error."), ptp_error);

	return ptp_error;
}

uint16_t CanonEOS::Capture()
{
	return Operation(PTP_OC_EOS_Capture, 0, NULL);
}

uint16_t CanonEOS::Test()
{
	uint16_t	ptp_error	= PTP_RC_GeneralError;

	if ((ptp_error = Operation(0x9802, 0, NULL)) != PTP_RC_OK)
		Message(PSTR("Test: Error: "), ptp_error);

	return ptp_error;
}

uint16_t CanonEOS::SetProperty(uint16_t prop, uint32_t val)
{
	uint16_t	ptp_error	= PTP_RC_GeneralError;
	OperFlags	flags		= { 0, 0, 1, 1, 3, 12 };
	uint32_t	params[3];

	params[0] = 0x0000000C;
	params[1] = (uint32_t)prop;
	params[2] = val;

	if ( (ptp_error = Transaction(PTP_OC_EOS_SetDevicePropValue, &flags, NULL, (void*)params)) != PTP_RC_OK)
		Message(PSTR("SetProperty: Error."), ptp_error);

	return ptp_error;
}
