#ifndef __PTPCALLBACK_H__
#define __PTPCALLBACK_H__

#include <inttypes.h>
#include <avr/pgmspace.h>
#include "WProgram.h"

// Base class for incomming data parser
class PTPReadParser
{
public:
	virtual void Parse(const uint16_t len, const uint8_t *pbuf, const uint32_t &offset) = 0;
};

struct MultiValueBuffer
{
	uint8_t		valueSize;
	void		*pValue;
};

class MultiByteValueParser
{
	uint8_t				*pBuf;
	uint8_t				countDown;
	uint8_t				valueSize;

public:
	MultiByteValueParser() : pBuf(NULL), countDown(0), valueSize(0) {};

	const uint8_t* GetBuffer() { return pBuf; };

	void Initialize(MultiValueBuffer *pbuf) 
	{ 
		pBuf = (uint8_t*)pbuf->pValue; 
		countDown = valueSize = pbuf->valueSize; 
	};

	bool Parse(uint8_t **pp, uint16_t *pcntdn);
};

class ByteSkipper
{
	uint8_t				*pBuf;
	uint8_t				nStage;
	uint8_t				countDown;

public:
	ByteSkipper() : pBuf(NULL), nStage(0), countDown(0) {};

	void Initialize(MultiValueBuffer *pbuf) 
	{ 
		pBuf = (uint8_t*)pbuf->pValue; 
		countDown = 0; 
	};

	bool Skip(uint8_t **pp, uint16_t *pcntdn, uint8_t bytes_to_skip)
	{
		switch (nStage)
		{
		case 0:
			countDown = bytes_to_skip;
			nStage ++;
		case 1:
			for (; countDown && (*pcntdn); countDown--, (*pp)++, (*pcntdn)--);

			if (!countDown)
				nStage = 0;
		};
		return (!countDown);
	};
};

// Pointer to a callback function triggered for each element of PTP array when used with PTPArrayParser
typedef void (*PTP_ARRAY_EL_FUNC)(MultiValueBuffer *p, uint32_t count/*, uint8_t level*/);

class PTPListParser
{
	uint8_t				nStage;
	uint32_t			arLen;
	uint32_t			arLenCntdn;
	uint8_t				lenSize;
	uint8_t				valSize;
	MultiValueBuffer	*pBuf;

	// The only parser for both size and array element parsing
	MultiByteValueParser				theParser;

public:

	enum {modeArray, modeRange};

	PTPListParser() : 
		pBuf(NULL), 
		nStage(0), 
		arLenCntdn(0), 
		arLen(0),
		lenSize(0),
		valSize(0)
		{};

	void Initialize(uint8_t len_size, uint8_t val_size, MultiValueBuffer *p, uint8_t mode = modeArray) 
	{ 
		pBuf	= p; 
		lenSize	= len_size;
		valSize = val_size;

		if (mode == modeRange)
		{
			arLenCntdn = arLen = 3;
			nStage = 2;
		}
		theParser.Initialize(p);
	};

	bool Parse(uint8_t **pp, uint16_t *pcntdn, PTP_ARRAY_EL_FUNC pf);
};


// Base class for outgoing data supplier
class PTPDataSupplier
{
public:
	virtual uint32_t GetDataSize() = 0;
	virtual void GetData(const uint16_t len, uint8_t *pbuf) = 0;
};

#endif // __PTPCALLBACK_H__
